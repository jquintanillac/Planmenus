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
        /// Ticket de venta en papel continuo de 58 u 80 mm
        /// </summary>
        public byte[] Ticket(VMTicket t)
        {
            float fuente = t.ancho_mm == 58 ? 7 : 8.5f;

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
                        col.Item().AlignCenter().Text(t.empresa).Bold().FontSize(fuente + 2.5f);
                        if (!string.IsNullOrWhiteSpace(t.ruc)) col.Item().AlignCenter().Text($"RUC: {t.ruc}");
                        if (!string.IsNullOrWhiteSpace(t.direccion)) col.Item().AlignCenter().Text(t.direccion);
                        if (!string.IsNullOrWhiteSpace(t.telefono)) col.Item().AlignCenter().Text($"Tel: {t.telefono}");

                        col.Item().PaddingVertical(2).LineHorizontal(0.5f);
                        col.Item().Text(txt => { txt.Span(t.pagado ? "TICKET N° " : "PRE-CUENTA N° ").Bold(); txt.Span(t.ticket.ToString("D6")); });
                        col.Item().Text($"Fecha: {t.fecha:dd/MM/yyyy HH:mm}");
                        if (!string.IsNullOrWhiteSpace(t.mesa)) col.Item().Text($"Mesa: {t.mesa}");
                        if (!string.IsNullOrWhiteSpace(t.delivery)) col.Item().Text($"Reparto: {t.delivery}");
                        if (!string.IsNullOrWhiteSpace(t.usuario)) col.Item().Text($"Atendió: {t.usuario}");
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
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("TOTAL").Bold().FontSize(fuente + 2);
                            row.AutoItem().Text(Soles(t.total)).Bold().FontSize(fuente + 2);
                        });
                        if (t.pagado && !string.IsNullOrWhiteSpace(t.tipo_pago)) col.Item().Text($"Pago: {t.tipo_pago}");
                        if (!t.pagado) col.Item().AlignCenter().Text("Pendiente de pago").Bold();
                        if (!string.IsNullOrWhiteSpace(t.observacion)) col.Item().Text($"Obs: {t.observacion}").Italic();

                        col.Item().PaddingTop(4).AlignCenter().Text(t.pie);
                        col.Item().AlignCenter().Text("Comprobante interno, no válido como boleta").FontSize(fuente - 1.5f).FontColor(Colors.Grey.Darken1);
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
                        col.Item().AlignCenter().Text(empresa).FontSize(12);
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
