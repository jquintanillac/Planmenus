using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Models;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Pantalla de cocina (KDS): comandas pendientes en orden de llegada, se actualiza sola.
    /// </summary>
    [Permiso("submenu3_9")]
    public class CocinaController : Controller
    {
        private readonly Datacontext _context;

        public CocinaController(Datacontext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await ComandasAsync());
        }

        // GET: Cocina/Comandas - fragmento HTML que la pantalla recarga cada pocos segundos
        public async Task<IActionResult> Comandas()
        {
            return PartialView("_Comandas", await ComandasAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Estado(int id, byte estado)
        {
            var caja = await _context.Cajas.FirstOrDefaultAsync(c => c.id_caja == id && c.caja_act);
            if (caja != null && estado is EstadoCocina.Listo or EstadoCocina.Entregado)
            {
                caja.caja_cocina = estado;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<VMCocina> ComandasAsync()
        {
            var hoy = Fechas.HoyPeru();
            var cajas = await (from c in _context.Cajas
                               where c.caja_act && c.caja_cocina != EstadoCocina.Entregado && c.caja_fecha >= hoy
                               join m in _context.Mesas on c.id_mesa equals m.id_mesa into mg
                               from m in mg.DefaultIfEmpty()
                               join d in _context.Deliveries on c.id_deliv equals d.id_deliv into dg
                               from d in dg.DefaultIfEmpty()
                               orderby c.caja_fecha
                               select new VMComanda
                               {
                                   id_caja = c.id_caja,
                                   desde = c.caja_fecha,
                                   estado = c.caja_cocina,
                                   mesa = m.mesa_desc,
                                   delivery = d.deliv_nombcom,
                                   obs = c.caja_obs,
                                   pagado = c.caja_pagado
                               }).ToListAsync();

            var ids = cajas.Select(c => c.id_caja).ToList();
            var items = await (from d in _context.Caja_Detalles
                               join m in _context.Menus on d.id_menu equals m.id_menu
                               where ids.Contains(d.id_caja)
                               orderby d.id_detalle
                               select new { d.id_caja, m.menu_desc, d.det_cant }).ToListAsync();
            foreach (var c in cajas)
            {
                c.platos = items.Where(i => i.id_caja == c.id_caja).Select(i => new VMTicketItem { descripcion = i.menu_desc, cantidad = i.det_cant }).ToList();
            }
            return new VMCocina { ahora = Fechas.AhoraPeru(), comandas = cajas };
        }
    }
}
