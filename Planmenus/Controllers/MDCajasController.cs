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

        // GET: MDCajas/Create - pantalla de venta. Con id_caja agrega platos a un pedido abierto; con id_mesa la preselecciona.
        [Permiso("submenu3_1")]
        public async Task<IActionResult> Create(int? id_caja, int? id_mesa, [FromServices] VentaService ventas, [FromServices] TurnoService turnos)
        {
            if (id_caja > 0)
            {
                var pedido = await ventas.PedidoAbiertoAsync(id_caja.Value);
                if (pedido == null)
                {
                    TempData["error"] = "El pedido ya fue cobrado o anulado.";
                    return RedirectToAction("Index", "Salon");
                }
                ViewBag.pedido = pedido;
                ViewBag.pedido_mesa = await _context.Mesas.Where(m => m.id_mesa == pedido.id_mesa).Select(m => m.mesa_desc).FirstOrDefaultAsync();
            }

            var hoy = Fechas.HoyPeru();
            ViewBag.menu = await _context.Menus.Where(x => x.menu_fecha.Date == hoy).OrderBy(x => x.menu_desc).ToListAsync();
            ViewBag.delivery = await _context.Deliveries.Where(x => x.deliv_act).ToListAsync();
            ViewBag.tipago = await _context.Tipo_Pagos.Where(x => x.tipago_act).ToListAsync();
            ViewBag.mesa = await _context.Mesas.Where(x => x.mesa_act).OrderBy(x => x.mesa_nro).ToListAsync();
            ViewBag.id_mesa = id_mesa;
            ViewBag.caja_abierta = await turnos.AbiertoAsync() != null;
            return View();
        }

        // POST: MDCajas/Create
        // accion=cobrar: se cobra y se imprime el ticket. accion=cocina: queda abierto para cobrarlo despues desde Salon.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("submenu3_1")]
        public async Task<IActionResult> Create(VMVenta venta, [FromServices] VentaService ventas, [FromServices] ImpresionService impresion)
        {
            var resultado = await ventas.RegistrarAsync(venta, SessionHelper.GetNameIdentifier(User));
            if (!resultado.Ok)
            {
                return BadRequest(resultado.Error);
            }
            if (venta.Cobrar && venta.id_caja == null)
            {
                var imp = await impresion.ImprimirVentaAsync(resultado.Caja.id_caja, alCobrar: true);
                if (imp.Mensaje != null) TempData[imp.Ok ? "success" : "error"] = imp.Mensaje;
                return RedirectToAction(nameof(Ticket), new { id = resultado.Caja.id_caja, imprimir = imp.Navegador });
            }
            TempData["success"] = $"Pedido N° {resultado.Caja.id_caja:D6} enviado a cocina.";
            return RedirectToAction("Pedido", "Salon", new { id = resultado.Caja.id_caja });
        }

        // GET: MDCajas/Ticket/5 - ticket en HTML (vista previa e impresion desde el navegador)
        [Permiso("submenu3_1", "submenu3_3")]
        public async Task<IActionResult> Ticket(int id, bool imprimir = false)
        {
            var ticket = await _tickets.ObtenerAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }
            ViewBag.imprimir = imprimir;
            return View(ticket);
        }

        // POST: MDCajas/Imprimir/5 - (re)imprime el ticket en la ticketera configurada
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("submenu3_1", "submenu3_3")]
        public async Task<IActionResult> Imprimir(int id, [FromServices] ImpresionService impresion)
        {
            var imp = await impresion.ImprimirVentaAsync(id, alCobrar: false);
            if (imp.Navegador)
            {
                return RedirectToAction(nameof(Ticket), new { id, imprimir = true });
            }
            TempData[imp.Ok ? "success" : "error"] = imp.Mensaje;
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
