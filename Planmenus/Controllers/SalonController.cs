using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Helpers;
using Planmenus.Models;
using Planmenus.Services;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Salon: mapa de mesas con su estado y gestion de los pedidos abiertos (por cobrar).
    /// </summary>
    [Permiso("submenu3_8")]
    public class SalonController : Controller
    {
        private readonly Datacontext _context;
        private readonly VentaService _ventas;

        public SalonController(Datacontext context, VentaService ventas)
        {
            _context = context;
            _ventas = ventas;
        }

        public async Task<IActionResult> Index(int? zona, [FromServices] PlanoService plano)
        {
            ViewBag.plano = await plano.ObtenerAsync(zona, conEstado: true);
            ViewBag.usuario = SessionHelper.GetNameIdentifier(User);

            var abiertos = await _context.Cajas.AsNoTracking()
                .Where(c => c.caja_act && !c.caja_pagado)
                .Select(c => new VMSalonPedido
                {
                    id_caja = c.id_caja,
                    id_mesa = c.id_mesa,
                    monto = c.caja_monto,
                    desde = c.caja_fecha,
                    cocina = c.caja_cocina,
                    obs = c.caja_obs,
                    platos = _context.Caja_Detalles.Where(d => d.id_caja == c.id_caja).Sum(d => (int?)d.det_cant) ?? 0
                })
                .ToListAsync();

            var mesas = await _context.Mesas.AsNoTracking().Where(m => m.mesa_act).OrderBy(m => m.mesa_nro).ToListAsync();
            var idsMesas = mesas.Select(m => m.id_mesa).ToHashSet();
            var vm = new VMSalon
            {
                ahora = Fechas.AhoraPeru(),
                mesas = mesas.Select(m => new VMSalonMesa
                {
                    id_mesa = m.id_mesa,
                    mesa_desc = m.mesa_desc,
                    mesa_nro = m.mesa_nro,
                    pedido = abiertos.Where(a => a.id_mesa == m.id_mesa).MaxBy(a => a.id_caja)
                }).ToList(),
                sin_mesa = abiertos.Where(a => a.id_mesa == null || !idsMesas.Contains(a.id_mesa.Value))
                                   .OrderBy(a => a.desde)
                                   .ToList()
            };
            return View(vm);
        }

        public async Task<IActionResult> Pedido(int id, [FromServices] TicketService tickets)
        {
            var ticket = await tickets.ObtenerAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }
            if (ticket.pagado || ticket.anulado)
            {
                return RedirectToAction("Ticket", "MDCajas", new { id });
            }
            ViewBag.tipago = await _context.Tipo_Pagos.Where(t => t.tipago_act).ToListAsync();
            ViewBag.cocina = await _context.Cajas.Where(c => c.id_caja == id).Select(c => c.caja_cocina).FirstAsync();
            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("submenu3_1")]
        public async Task<IActionResult> Cobrar(int id, int? id_tipago, [FromServices] ImpresionService impresion)
        {
            var r = await _ventas.CobrarAsync(id, id_tipago);
            if (!r.Ok)
            {
                TempData["error"] = r.Error;
                return r.Caja == null ? RedirectToAction(nameof(Index)) : RedirectToAction(nameof(Pedido), new { id });
            }
            var imp = await impresion.ImprimirVentaAsync(id, alCobrar: true);
            TempData[imp.Ok ? "success" : "error"] = imp.Mensaje ?? $"Pedido N° {id:D6} cobrado.";
            return RedirectToAction("Ticket", "MDCajas", new { id, imprimir = imp.Navegador });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("submenu3_1")]
        public async Task<IActionResult> Quitar(int id, int id_detalle)
        {
            var r = await _ventas.QuitarPlatoAsync(id, id_detalle);
            if (!r.Ok)
            {
                TempData["error"] = r.Error;
            }
            return RedirectToAction(nameof(Pedido), new { id });
        }
    }
}
