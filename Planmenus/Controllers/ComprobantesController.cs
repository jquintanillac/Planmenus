using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Services.Facturacion;
using System.IO.Compression;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Comprobantes electronicos emitidos: estado ante SUNAT, descargas, reenvio, anulacion
    /// y emision de boleta o factura para ventas cobradas con ticket.
    /// </summary>
    [Permiso("submenu3_13")]
    public class ComprobantesController : Controller
    {
        private readonly Datacontext _context;
        private readonly FacturacionService _fe;

        public ComprobantesController(Datacontext context, FacturacionService fe)
        {
            _context = context;
            _fe = fe;
        }

        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, string tipo, byte? estado)
        {
            var hoy = Fechas.HoyPeru();
            var d = (desde ?? hoy.AddDays(-7)).Date;
            var h = (hasta ?? hoy).Date.AddDays(1);
            var q = _context.Comprobantes.AsNoTracking().Where(c => c.comp_fecha >= d && c.comp_fecha < h);
            if (!string.IsNullOrEmpty(tipo)) q = q.Where(c => c.comp_tipo == tipo);
            if (estado != null) q = q.Where(c => c.comp_estado == estado);
            ViewBag.desde = d;
            ViewBag.hasta = h.AddDays(-1);
            ViewBag.tipo = tipo;
            ViewBag.estado = estado;
            ViewBag.config = await _fe.ConfigAsync();
            ViewBag.pendientes = await _context.Comprobantes.CountAsync(c => c.comp_estado == EstadoComprobante.PorEnviar || c.comp_estado == EstadoComprobante.ErrorEnvio);
            var lista = await q.OrderByDescending(c => c.id_comp).Take(300)
                .Select(c => new MDComprobante
                {
                    id_comp = c.id_comp, id_caja = c.id_caja, comp_tipo = c.comp_tipo, comp_serie = c.comp_serie, comp_numero = c.comp_numero,
                    comp_fecha = c.comp_fecha, cli_tipo_doc = c.cli_tipo_doc, cli_num_doc = c.cli_num_doc, cli_nombre = c.cli_nombre,
                    comp_total = c.comp_total, comp_igv = c.comp_igv, comp_estado = c.comp_estado, comp_sunat_codigo = c.comp_sunat_codigo,
                    comp_sunat_mensaje = c.comp_sunat_mensaje, comp_ambiente = c.comp_ambiente, ref_id_comp = c.ref_id_comp, comp_intentos = c.comp_intentos
                }).ToListAsync();
            return View(lista);
        }

        public async Task<IActionResult> Xml(int id)
        {
            var c = await _context.Comprobantes.AsNoTracking().FirstOrDefaultAsync(x => x.id_comp == id);
            if (c?.comp_xml == null) return NotFound();
            var fe = await _fe.ConfigAsync();
            return File(c.comp_xml, "application/xml", UblService.Nombre(fe.fe_ruc, c) + ".xml");
        }

        public async Task<IActionResult> Cdr(int id)
        {
            var c = await _context.Comprobantes.AsNoTracking().FirstOrDefaultAsync(x => x.id_comp == id);
            if (c?.comp_cdr == null) return NotFound();
            var fe = await _fe.ConfigAsync();
            return File(c.comp_cdr, "application/zip", "R-" + UblService.Nombre(fe.fe_ruc, c) + ".zip");
        }

        /// <summary>XML y CDR de los comprobantes filtrados en un ZIP (para el contador)</summary>
        public async Task<IActionResult> Descargar(DateTime desde, DateTime hasta)
        {
            var fe = await _fe.ConfigAsync();
            var h = hasta.Date.AddDays(1);
            var lista = await _context.Comprobantes.AsNoTracking()
                .Where(c => c.comp_fecha >= desde.Date && c.comp_fecha < h && c.comp_xml != null)
                .OrderBy(c => c.id_comp).ToListAsync();
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                foreach (var c in lista)
                {
                    var nombre = UblService.Nombre(fe.fe_ruc, c);
                    using (var s = zip.CreateEntry(nombre + ".xml").Open()) await s.WriteAsync(c.comp_xml);
                    if (c.comp_cdr != null)
                    {
                        using var s2 = zip.CreateEntry("R-" + nombre + ".zip").Open();
                        await s2.WriteAsync(c.comp_cdr);
                    }
                }
            }
            return File(ms.ToArray(), "application/zip", $"comprobantes-{desde:yyyyMMdd}-{hasta:yyyyMMdd}.zip");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reenviar(int id)
        {
            var r = await _fe.ReenviarAsync(id);
            TempData[r.Ok ? "success" : "error"] = r.Ok ? $"{r.Comprobante.Numero}: {EstadoComprobante.Nombre(r.Comprobante.comp_estado)} por SUNAT." : r.Error;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReenviarPendientes()
        {
            int ok = 0, total = 0;
            foreach (var id in await _fe.PorReenviarAsync())
            {
                total++;
                if ((await _fe.ReenviarAsync(id)).Ok) ok++;
            }
            TempData[ok == total ? "success" : "error"] = $"Se reenviaron {total} comprobante(s): {ok} aceptado(s).";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Anular(int id, string sustento)
        {
            var r = await _fe.AnularAsync(id, sustento);
            TempData[r.Ok ? "success" : "error"] = r.Ok
                ? (r.Comprobante.comp_tipo == TipoComprobante.NotaCredito ? $"Se emitió la nota de crédito {r.Comprobante.Numero}." : "Comprobante anulado.")
                : r.Error;
            return RedirectToAction(nameof(Index));
        }

        // GET: Comprobantes/Emitir/5 - boleta o factura para una venta cobrada con ticket
        public async Task<IActionResult> Emitir(int id)
        {
            var caja = await _context.Cajas.AsNoTracking().FirstOrDefaultAsync(c => c.id_caja == id && c.caja_act && c.caja_pagado);
            if (caja == null) return NotFound();
            ViewBag.caja = caja;
            ViewBag.fe = await _fe.ConfigAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Emitir(int id, Models.VMVenta datos)
        {
            var r = await _fe.EmitirVentaAsync(id, new FacturacionService.Cliente(datos.comp_tipo, datos.cli_tipo_doc, datos.cli_num_doc, datos.cli_nombre, datos.cli_direccion, datos.cli_email));
            if (!r.Ok && r.Comprobante == null)
            {
                TempData["error"] = r.Error;
                return RedirectToAction(nameof(Emitir), new { id });
            }
            TempData[r.Ok ? "success" : "error"] = r.Ok ? $"{r.Comprobante.Numero} aceptado por SUNAT." : r.Error;
            return RedirectToAction("Ticket", "MDCajas", new { id });
        }
    }
}
