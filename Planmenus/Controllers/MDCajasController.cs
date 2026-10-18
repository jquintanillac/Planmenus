#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Models;
using Planmenus.Data.DataSql;
using Planmenus.Helpers;
using Planmenus.Services;
using System.Net.Sockets;

namespace Planmenus.Controllers
{
    public class MDCajasController : Controller
    {
        private readonly Datacontext _context;
        private readonly SP_Menus _menus;
        private readonly TicketService _tickets;
        
        public MDCajasController(Datacontext context, SP_Menus menus, TicketService tickets)
        {
            _context = context;
            _menus = menus;
            _tickets = tickets;
        }

        // GET: MDCajas
        [Permiso("submenu3_2")]
        public async Task<IActionResult> Index(DateTime Fecini, DateTime Fecfin)
        {
            DateTime fecha1 = Fechas.HoyPeru();
            DateTime fecha2 = fecha1;

            if (Fecini == DateTime.MinValue && Fecfin == DateTime.MinValue)
            {
                Fecini = fecha1;
                Fecfin = fecha2;
            }
            return View();
        }

        // GET: MDCajas
        [Permiso("submenu3_2")]
        public async Task<IActionResult> Cuadre()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("submenu3_3")]
        public async Task<IActionResult> Anular(int id, string caja_obs)
        {
            var mDCaja = await _context.Cajas.FindAsync(id);
            if (mDCaja == null)
            {
                return NotFound();
            }

            mDCaja.caja_act = false;
            mDCaja.caja_obs = caja_obs;
            await _context.SaveChangesAsync();
            return RedirectToAction("Cajadiaria", "MDCajas");
        }

        // GET: MDCajas
        [Permiso("submenu3_3")]
        public async Task<IActionResult> Cajadiaria(DateTime Fecini, DateTime Fecfin)
        {

            DateTime fecha1 = Fechas.HoyPeru();
            DateTime fecha2 = fecha1;

            if (Fecini == DateTime.MinValue && Fecfin == DateTime.MinValue)
            {
                Fecini = fecha1;
                Fecfin = fecha2;
            }
            ViewBag.cajas = await _menus.mSPcajas(Fecini, Fecfin);
            return View();
        }

        [Permiso("submenu3_3")]
        public IActionResult Imprimir(int? id)
        {
            return RedirectToAction(nameof(Ticket), new { id, imprimir = true });
        }

        [Permiso("submenu3_4")]
        public async Task<IActionResult> RptListpagos(DateTime Fecini, DateTime Fecfin)
        {
            DateTime fecha1 = Fechas.HoyPeru();
            DateTime fecha2 = fecha1;

            if(Fecini == DateTime.MinValue && Fecfin == DateTime.MinValue)
            {
                Fecini = fecha1;
                Fecfin = fecha2;
            }

            ViewBag.reporteQ01 = await _menus.mReporteQ01(Fecini, Fecfin);
            return View();
        }

        [Permiso("submenu3_5")]
        public async Task<IActionResult> Rpttipodepago(DateTime Fecini, DateTime Fecfin)
        {
            DateTime fecha1 = Fechas.HoyPeru();
            DateTime fecha2 = fecha1;
            if (Fecini == DateTime.MinValue && Fecfin == DateTime.MinValue)
            {
                Fecini = fecha1;
                Fecfin = fecha2;
            }
            ViewBag.reporteQ02 = await _menus.mReporteQ02(Fecini, Fecfin);
            return View();
        }

        [Permiso("submenu3_6")]
        public async Task<IActionResult> Rptporplato(DateTime Fecini, DateTime Fecfin)
        {
            DateTime fecha1 = Fechas.HoyPeru();
            DateTime fecha2 = fecha1;
            if (Fecini == DateTime.MinValue && Fecfin == DateTime.MinValue)
            {
                Fecini = fecha1;
                Fecfin = fecha2;
            }
            ViewBag.reporteQ03 = await _menus.mReporteQ03(Fecini, Fecfin);
            return View();
        }
        [Permiso("submenu3_7")]
        public async Task<IActionResult> Rptcantplato(DateTime Fecini, DateTime Fecfin)
        {
            DateTime fecha1 = Fechas.HoyPeru();
            DateTime fecha2 = fecha1;
            if (Fecini == DateTime.MinValue && Fecfin == DateTime.MinValue)
            {
                Fecini = fecha1;
                Fecfin = fecha2;
            }
            ViewBag.reporteQ04 = await _menus.mReporteQ04(Fecini, Fecfin);
            return View();
        }

        // GET: MDCajas/Details/5
        [Permiso("submenu3_3")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDCaja = await _context.Cajas
                .FirstOrDefaultAsync(m => m.id_caja == id);
            if (mDCaja == null)
            {
                return NotFound();
            }

            return View(mDCaja);
        }

        // GET: MDCajas/Create
        [Permiso("submenu3_1")]
        public async Task<IActionResult> Create()
        {
            var hoy = Fechas.HoyPeru();
            ViewBag.menu = await _context.Menus.Where(x => x.menu_fecha.Date == hoy).OrderBy(x => x.menu_desc).ToListAsync();
            ViewBag.delivery = await _context.Deliveries.Where(x => x.deliv_act).ToListAsync();
            ViewBag.tipago = await _context.Tipo_Pagos.Where(x => x.tipago_act).ToListAsync();
            ViewBag.mesa = await _context.Mesas.Where(x => x.mesa_act).OrderBy(x => x.mesa_nro).ToListAsync();
            return View();
        }

        // POST: MDCajas/Create
        // Registra un pedido con uno o varios platos. El total se calcula con los precios de la base de datos.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("submenu3_1")]
        public async Task<IActionResult> Create(VMVenta venta)
        {
            var items = (venta.items ?? new())
                .Where(i => i.cantidad > 0)
                .GroupBy(i => i.id_menu)
                .Select(g => new VMVentaItem { id_menu = g.Key, cantidad = g.Sum(i => i.cantidad) })
                .ToList();
            if (items.Count == 0 || items.Any(i => i.cantidad > 999))
            {
                return BadRequest("El pedido no tiene platos válidos.");
            }

            var hoy = Fechas.HoyPeru();
            var ids = items.Select(i => i.id_menu).ToList();
            var precios = await _context.Menus
                .Where(m => ids.Contains(m.id_menu) && m.menu_fecha.Date == hoy)
                .ToDictionaryAsync(m => m.id_menu, m => m.menu_precio);
            if (precios.Count != ids.Count || !await _context.Tipo_Pagos.AnyAsync(t => t.id_tipago == venta.id_tipago))
            {
                return BadRequest("El pedido contiene platos o tipo de pago no disponibles.");
            }

            var detalle = items.Select(i => new MDCaja_Detalle
            {
                id_menu = i.id_menu,
                det_cant = i.cantidad,
                det_precio = precios[i.id_menu],
                det_subtotal = precios[i.id_menu] * i.cantidad
            }).ToList();

            var oCaja = new MDCaja
            {
                id_tipago = venta.id_tipago,
                id_mesa = venta.id_mesa > 0 ? venta.id_mesa : null,
                id_deliv = venta.id_deliv > 0 ? venta.id_deliv : null,
                caja_obs = venta.caja_obs,
                caja_monto = detalle.Sum(d => d.det_subtotal),
                caja_fecha = Fechas.AhoraPeru(),
                caja_usuario = SessionHelper.GetNameIdentifier(User),
                caja_act = true
            };

            await using var tx = await _context.Database.BeginTransactionAsync();
            _context.Cajas.Add(oCaja);
            await _context.SaveChangesAsync();
            detalle.ForEach(d => d.id_caja = oCaja.id_caja);
            _context.Caja_Detalles.AddRange(detalle);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            return RedirectToAction(nameof(Ticket), new { id = oCaja.id_caja, imprimir = true });
        }

        // GET: MDCajas/Ticket/5 - ticket en HTML listo para la ticketera (impresion desde el navegador)
        [Permiso("submenu3_1", "submenu3_3")]
        public async Task<IActionResult> Ticket(int id, bool imprimir = false)
        {
            var ticket = await _tickets.ObtenerAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }
            var empresa = await _tickets.ObtenerEmpresaAsync();
            ViewBag.imprimir = imprimir;
            ViewBag.termica = !string.IsNullOrWhiteSpace(empresa.gene_impresora_ip);
            return View(ticket);
        }

        // POST: MDCajas/ImprimirTermica/5 - envia el ticket por red a la ticketera ESC/POS configurada
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("submenu3_1", "submenu3_3")]
        public async Task<IActionResult> ImprimirTermica(int id, [FromServices] EscPosService escpos, [FromServices] ILogger<MDCajasController> logger)
        {
            var ticket = await _tickets.ObtenerAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }
            var empresa = await _tickets.ObtenerEmpresaAsync();
            if (string.IsNullOrWhiteSpace(empresa.gene_impresora_ip))
            {
                TempData["error"] = "No hay una ticketera de red configurada en Generales.";
                return RedirectToAction(nameof(Ticket), new { id });
            }

            try
            {
                await escpos.ImprimirAsync(ticket, empresa.gene_impresora_ip, empresa.gene_impresora_puerto);
                TempData["success"] = "Ticket enviado a la ticketera.";
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException or IOException)
            {
                logger.LogWarning(ex, "No se pudo imprimir en {Ip}:{Puerto}", empresa.gene_impresora_ip, empresa.gene_impresora_puerto);
                TempData["error"] = $"No se pudo conectar con la ticketera {empresa.gene_impresora_ip}:{empresa.gene_impresora_puerto}.";
            }
            return RedirectToAction(nameof(Ticket), new { id });
        }

        // GET: MDCajas/Edit/5
        [Permiso("submenu3_3")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDCaja = await _context.Cajas.FindAsync(id);
            if (mDCaja == null)
            {
                return NotFound();
            }
            return View(mDCaja);
        }

        // GET: MDCajas/Delete/5
        [Permiso("submenu3_3")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDCaja = await _context.Cajas
                .FirstOrDefaultAsync(m => m.id_caja == id);
            if (mDCaja == null)
            {
                return NotFound();
            }

            return View(mDCaja);
        }

        // POST: MDCajas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Permiso("submenu3_3")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDCaja = await _context.Cajas.FindAsync(id);
            _context.Cajas.Remove(mDCaja);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
