using Planmenus.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;

namespace Planmenus.Services
{
    /// <summary>
    /// Genera los PDF del sistema con QuestPDF (reemplaza a Rotativa/wkhtmltopdf).
    /// </summary>
    public class PdfService
    {
        private static readonly CultureInfo Peru = CultureInfo.GetCultureInfo("es-PE");

        private static string Soles(decimal monto) => "S/ " + monto.ToString("N2", Peru);

        /// <summary>
        /// Deja solo caracteres que la fuente del PDF (Lato) puede dibujar: latinos con tildes y ñ, signos comunes y €.
        /// QuestPDF falla si encuentra un caracter sin glifo (por ejemplo un emoji escrito en un plato).
        /// </summary>
        public static string Limpio(string texto)
        {
            if (string.IsNullOrEmpty(texto))
            {
                return texto;
            }
            var sb = new System.Text.StringBuilder(texto.Length);
            foreach (var c in texto)
            {
                if (c == '\n' || (c >= 0x20 && c <= 0x7E) || (c >= 0xA0 && c <= 0x17F) || (c >= 0x2010 && c <= 0x2027) || c == '€' || c == '™')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString().Trim();
        }

        private static void Limpiar(VMTicket t)
        {
            t.empresa = Limpio(t.empresa); t.ruc = Limpio(t.ruc); t.direccion = Limpio(t.direccion); t.telefono = Limpio(t.telefono);
            t.comprobante = Limpio(t.comprobante); t.cliente = Limpio(t.cliente); t.cliente_doc = Limpio(t.cliente_doc);
            t.mesa = Limpio(t.mesa); t.delivery = Limpio(t.delivery); t.usuario = Limpio(t.usuario); t.tipo_pago = Limpio(t.tipo_pago);
            t.observacion = Limpio(t.observacion); t.pie = Limpio(t.pie);
            t.items.ForEach(i => i.descripcion = Limpio(i.descripcion));
            t.leyendas = t.leyendas.Select(Limpio).ToList();
            t.subtotales = t.subtotales.Select(s => (Limpio(s.etiqueta), s.monto)).ToList();
        }

        /// <summary>
        /// Ticket de venta en papel continuo de 58 u 80 mm
        /// </summary>
        public byte[] Ticket(VMTicket t)
        {
            Limpiar(t);
            var f = t.formato ?? new Data.Entities.MDTicketera();
            float fuente = t.ancho_mm == 58 ? 7 : 8.5f;
            byte[] qr = string.IsNullOrWhiteSpace(t.qr) ? null
                : new QRCoder.PngByteQRCode(new QRCoder.QRCodeGenerator().CreateQrCode(t.qr, QRCoder.QRCodeGenerator.ECCLevel.M)).GetGraphic(8);

            return Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.ContinuousSize(t.ancho_mm, Unit.Millimetre);
                    page.Margin(3, Unit.Millimetre);
                    page.DefaultTextStyle(x => x.FontSize(fuente));

                    page.Content().Column(col =>
                    {
                        col.Spacing(2);
                        if (f.tk_logo_mostrar && f.tk_logo_png?.Length > 0)
                        {
                            col.Item().AlignCenter().MaxWidth(t.ancho_mm == 58 ? 110 : 150).MaxHeight(80).Image(f.tk_logo_png).FitArea();
                        }
                        col.Item().AlignCenter().Text(t.empresa).Bold().FontSize(fuente + 2.5f);
                        if (f.tk_mostrar_ruc && !string.IsNullOrWhiteSpace(t.ruc)) col.Item().AlignCenter().Text($"RUC: {t.ruc}");
                        if (f.tk_mostrar_direccion && !string.IsNullOrWhiteSpace(t.direccion)) col.Item().AlignCenter().Text(t.direccion);
                        if (f.tk_mostrar_telefono && !string.IsNullOrWhiteSpace(t.telefono)) col.Item().AlignCenter().Text($"Tel: {t.telefono}");
                        if (!string.IsNullOrWhiteSpace(t.comprobante))
                        {
                            col.Item().PaddingTop(2).AlignCenter().Text(t.comprobante).Bold();
                        }

                        col.Item().PaddingVertical(2).LineHorizontal(0.5f);
                        if (string.IsNullOrWhiteSpace(t.comprobante))
                        {
                            col.Item().Text(txt => { txt.Span(t.pagado ? "TICKET N° " : "PRE-CUENTA N° ").Bold(); txt.Span(t.ticket.ToString("D6")); });
                        }
                        col.Item().Text($"Fecha: {t.fecha:dd/MM/yyyy HH:mm}");
                        if (!string.IsNullOrWhiteSpace(t.cliente)) col.Item().Text($"Cliente: {t.cliente}");
                        if (!string.IsNullOrWhiteSpace(t.cliente_doc)) col.Item().Text(t.cliente_doc);
                        if (f.tk_mostrar_mesa && !string.IsNullOrWhiteSpace(t.mesa)) col.Item().Text($"Mesa: {t.mesa}");
                        if (f.tk_mostrar_mesa && !string.IsNullOrWhiteSpace(t.delivery)) col.Item().Text($"Reparto: {t.delivery}");
                        if (f.tk_mostrar_mozo && !string.IsNullOrWhiteSpace(t.usuario)) col.Item().Text($"Atendió: {t.usuario}");
                        if (t.anulado) col.Item().AlignCenter().Text("*** ANULADO ***").Bold().FontColor(Colors.Red.Medium);

                        col.Item().PaddingVertical(2).LineHorizontal(0.5f);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(t.ancho_mm == 58 ? 20 : 25);
                                c.RelativeColumn();
                                c.ConstantColumn(t.ancho_mm == 58 ? 38 : 48);
                            });
                            table.Header(h =>
                            {
                                h.Cell().Text("Cant").Bold();
                                h.Cell().Text("Descripción").Bold();
                                h.Cell().AlignRight().Text("Importe").Bold();
                            });
                            foreach (var item in t.items)
                            {
                                table.Cell().Text(item.cantidad.ToString());
                                table.Cell().Text(txt =>
                                {
                                    txt.Line(item.descripcion);
                                    if (item.cantidad > 1) txt.Span($"{item.cantidad} x {Soles(item.precio)}").FontSize(fuente - 1).FontColor(Colors.Grey.Darken1);
                                });
                                table.Cell().AlignRight().Text(Soles(item.subtotal));
                            }
                        });

                        col.Item().PaddingVertical(2).LineHorizontal(0.5f);
                        foreach (var (etiqueta, monto) in t.subtotales)
                        {
                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text(etiqueta);
                                row.AutoItem().Text(Soles(monto));
                            });
                        }
                        col.Item().Row(row =>
                        {
                            var tam = f.tk_total_grande ? fuente + 2 : fuente;
                            row.RelativeItem().Text("TOTAL").Bold().FontSize(tam);
                            row.AutoItem().Text(Soles(t.total)).Bold().FontSize(tam);
                        });
                        if (t.pagado && !string.IsNullOrWhiteSpace(t.tipo_pago)) col.Item().Text($"Pago: {t.tipo_pago}");
                        if (!t.pagado) col.Item().AlignCenter().Text("Pendiente de pago").Bold();
                        if (f.tk_mostrar_obs && !string.IsNullOrWhiteSpace(t.observacion)) col.Item().Text($"Obs: {t.observacion}").Italic();

                        if (qr != null)
                        {
                            col.Item().PaddingTop(4).AlignCenter().Width(t.ancho_mm == 58 ? 70 : 85).Image(qr);
                        }
                        foreach (var leyenda in t.leyendas)
                        {
                            col.Item().AlignCenter().Text(leyenda).FontSize(fuente - 1);
                        }
                        col.Item().PaddingTop(4).AlignCenter().Text(t.pie);
                        if (string.IsNullOrWhiteSpace(t.comprobante))
                        {
                            col.Item().AlignCenter().Text("Comprobante interno, no válido como boleta").FontSize(fuente - 1.5f).FontColor(Colors.Grey.Darken1);
                        }
                    });
                });
            }).GeneratePdf();
        }

        /// <summary>
        /// Cuadre de caja por dia en hoja A4
        /// </summary>
        public byte[] Cuadre(string empresa, DateTime desde, DateTime hasta, List<VMCaja_Q05> dias)
        {
            return Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("CUADRE DE CAJA").Bold().FontSize(16);
                        col.Item().AlignCenter().Text(Limpio(empresa) ?? "").FontSize(12);
                        col.Item().AlignCenter().Text($"Del {desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}").FontColor(Colors.Grey.Darken2);
                        col.Item().PaddingTop(8).LineHorizontal(1);
                    });

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.2f);
                            for (int i = 0; i < 5; i++) c.RelativeColumn();
                        });

                        static IContainer Celda(IContainer c) => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingVertical(4);
                        static IContainer Cabecera(IContainer c) => c.Background(Colors.Grey.Lighten3).PaddingVertical(5).PaddingHorizontal(2);

                        table.Header(h =>
                        {
                            foreach (var titulo in new[] { "Fecha", "Inicial", "Ventas", "Ingresos", "Egresos", "Total" })
                            {
                                var celda = h.Cell().Element(Cabecera);
                                (titulo == "Fecha" ? celda : celda.AlignRight()).Text(titulo).Bold();
                            }
                        });

                        foreach (var d in dias)
                        {
                            table.Cell().Element(Celda).Text(d.fecha);
                            table.Cell().Element(Celda).AlignRight().Text(Soles(d.Inicial));
                            table.Cell().Element(Celda).AlignRight().Text(Soles(d.Ventas));
                            table.Cell().Element(Celda).AlignRight().Text(Soles(d.Ingresos));
                            table.Cell().Element(Celda).AlignRight().Text(Soles(d.Egresos));
                            table.Cell().Element(Celda).AlignRight().Text(Soles(d.Total)).Bold();
                        }

                        table.Cell().Element(Cabecera).Text("TOTAL").Bold();
                        table.Cell().Element(Cabecera).AlignRight().Text(Soles(dias.Sum(d => d.Inicial))).Bold();
                        table.Cell().Element(Cabecera).AlignRight().Text(Soles(dias.Sum(d => d.Ventas))).Bold();
                        table.Cell().Element(Cabecera).AlignRight().Text(Soles(dias.Sum(d => d.Ingresos))).Bold();
                        table.Cell().Element(Cabecera).AlignRight().Text(Soles(dias.Sum(d => d.Egresos))).Bold();
                        table.Cell().Element(Cabecera).AlignRight().Text(Soles(dias.Sum(d => d.Total))).Bold();
                    });

                    page.Footer().AlignRight().Text(txt =>
                    {
                        txt.Span("Página ");
                        txt.CurrentPageNumber();
                        txt.Span(" de ");
                        txt.TotalPages();
                    });
                });
            }).GeneratePdf();
        }
    }
}
