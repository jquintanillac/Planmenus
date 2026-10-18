using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Models;

namespace Planmenus.Services
{
    /// <summary>
    /// Reune los datos de una venta y de la empresa para imprimir el ticket.
    /// </summary>
    public class TicketService
    {
        private readonly Datacontext _context;

        public TicketService(Datacontext context)
        {
            _context = context;
        }

        public async Task<MDGenerales> ObtenerEmpresaAsync()
        {
            return await _context.Generales.AsNoTracking().OrderBy(g => g.id_gene).FirstOrDefaultAsync()
                   ?? new MDGenerales { gene_empre = "MENU" };
        }

        /// <summary>Configuracion de la ticketera (o la predeterminada si aun no se guardo)</summary>
        public async Task<MDTicketera> ObtenerFormatoAsync()
        {
            return await _context.Ticketeras.AsNoTracking().OrderBy(t => t.id_ticketera).FirstOrDefaultAsync() ?? new MDTicketera();
        }

        public async Task<VMTicket> ObtenerAsync(int id_caja)
        {
            var caja = await _context.Cajas.AsNoTracking().FirstOrDefaultAsync(c => c.id_caja == id_caja);
            if (caja == null)
            {
                return null;
            }

            var items = await (from d in _context.Caja_Detalles
                               join m in _context.Menus on d.id_menu equals m.id_menu
                               where d.id_caja == id_caja
                               orderby d.id_detalle
                               select new VMTicketItem
                               {
                                   id_detalle = d.id_detalle,
                                   descripcion = m.menu_desc,
                                   cantidad = d.det_cant,
                                   precio = d.det_precio,
                                   subtotal = d.det_subtotal
                               }).ToListAsync();

            // Ventas anteriores al detalle: un solo menu por ticket
            if (items.Count == 0 && caja.id_menu != null)
            {
                var menu = await _context.Menus.AsNoTracking().FirstOrDefaultAsync(m => m.id_menu == caja.id_menu);
                items.Add(new VMTicketItem { descripcion = menu?.menu_desc ?? "Menu", cantidad = 1, precio = caja.caja_monto, subtotal = caja.caja_monto });
            }

            var empresa = await ObtenerEmpresaAsync();
            var formato = await ObtenerFormatoAsync();
            var comprobante = await _context.Comprobantes.AsNoTracking()
                .Where(c => c.id_caja == id_caja && c.comp_tipo != TipoComprobante.NotaCredito && c.comp_estado != EstadoComprobante.Rechazado)
                .OrderByDescending(c => c.id_comp).FirstOrDefaultAsync();
            var ticket = new VMTicket
            {
                ticket = caja.id_caja,
                fecha = caja.caja_fecha,
                usuario = caja.caja_mozo ?? caja.caja_usuario,
                tipo_pago = await _context.Tipo_Pagos.Where(t => t.id_tipago == caja.id_tipago).Select(t => t.tipago_desc).FirstOrDefaultAsync(),
                mesa = await _context.Mesas.Where(m => m.id_mesa == caja.id_mesa).Select(m => m.mesa_desc).FirstOrDefaultAsync(),
                delivery = await _context.Deliveries.Where(d => d.id_deliv == caja.id_deliv).Select(d => d.deliv_nombcom).FirstOrDefaultAsync(),
                observacion = caja.caja_obs,
                anulado = !caja.caja_act,
                pagado = caja.caja_pagado,
                total = caja.caja_monto,
                items = items,
                empresa = empresa.gene_empre,
                ruc = empresa.gene_ruc,
                direccion = empresa.gene_direc,
                telefono = empresa.gene_telef,
                pie = string.IsNullOrWhiteSpace(formato.tk_pie) ? "¡Gracias por su preferencia!" : formato.tk_pie,
                ancho_mm = formato.tk_ancho_mm == 58 ? 58 : 80,
                formato = formato
            };
            if (comprobante != null)
            {
                await CompletarComprobanteAsync(ticket, comprobante);
            }
            return ticket;
        }

        /// <summary>Datos de la representacion impresa del comprobante electronico</summary>
        private async Task CompletarComprobanteAsync(VMTicket t, MDComprobante c)
        {
            var fe = await _context.Facturacion.AsNoTracking().OrderBy(f => f.id_fe).FirstOrDefaultAsync() ?? new MDFacturacion();
            var nombre = TipoComprobante.Nombre(c.comp_tipo);
            t.empresa = string.IsNullOrWhiteSpace(fe.fe_nombre_comercial) ? fe.fe_razon_social : fe.fe_nombre_comercial;
            t.ruc = fe.fe_ruc;
            t.direccion = string.IsNullOrWhiteSpace(fe.fe_nombre_comercial) ? fe.fe_direccion : $"{fe.fe_razon_social} · {fe.fe_direccion}";
            t.comprobante = $"{nombre} {c.Numero}";
            t.fecha = c.comp_fecha;
            t.cliente = c.cli_nombre;
            if (c.cli_num_doc != "-")
            {
                t.cliente_doc = (c.cli_tipo_doc switch { "6" => "RUC", "1" => "DNI", "4" => "C.E.", "7" => "Pasaporte", _ => "Doc." }) + ": " + c.cli_num_doc;
            }
            t.subtotales = new() { ("Op. gravada", c.comp_gravada), ($"IGV ({c.comp_tasa_igv:0.##}%)", c.comp_igv) };
            t.total = c.comp_total;
            t.qr = Planmenus.Services.Facturacion.FacturacionService.Qr(fe.fe_ruc, c);
            t.leyendas = new()
            {
                "SON: " + Helpers.NumeroLetras.Soles(c.comp_total),
                $"Representación impresa de la {nombre.ToLowerInvariant()}",
                "Consulte su validez en www.sunat.gob.pe",
                "Resumen: " + c.comp_hash
            };
            if (c.comp_ambiente == AmbienteFe.Pruebas)
            {
                t.leyendas.Insert(0, "*** AMBIENTE DE PRUEBAS - SIN VALOR TRIBUTARIO ***");
            }
            if (c.comp_estado == EstadoComprobante.Anulado)
            {
                t.anulado = true;
            }
        }
    }
}
