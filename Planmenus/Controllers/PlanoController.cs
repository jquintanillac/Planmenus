using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Services;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Editor del plano del salon: zonas, ubicacion, forma, tamano y capacidad de cada mesa.
    /// </summary>
    [Permiso("submenu2_10")]
    public class PlanoController : Controller
    {
        private readonly Datacontext _context;
        private readonly PlanoService _plano;

        public PlanoController(Datacontext context, PlanoService plano)
        {
            _context = context;
            _plano = plano;
        }

        public async Task<IActionResult> Index(int? zona)
        {
            return View(await _plano.ObtenerAsync(zona, conEstado: false));
        }

        public class MesaPlano
        {
            public int id { get; set; }
            public int nro { get; set; }
            public string desc { get; set; }
            public int cap { get; set; }
            public string forma { get; set; }
            public int x { get; set; }
            public int y { get; set; }
            public int w { get; set; }
            public int h { get; set; }
            public int rot { get; set; }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar([FromBody] List<MesaPlano> mesas)
        {
            if (mesas == null)
            {
                return BadRequest("Datos inválidos.");
            }
            var ids = mesas.Select(m => m.id).ToList();
            var existentes = await _context.Mesas.Where(m => ids.Contains(m.id_mesa)).ToListAsync();
            foreach (var d in mesas)
            {
                var m = existentes.FirstOrDefault(e => e.id_mesa == d.id);
                if (m == null)
                {
                    continue;
                }
                m.mesa_forma = FormaMesa.Todas.Contains(d.forma) ? d.forma : FormaMesa.Cuadrada;
                m.mesa_ancho = Math.Clamp(d.w, 40, 400);
                m.mesa_alto = Math.Clamp(m.mesa_forma == FormaMesa.Redonda ? d.w : d.h, 40, 400);
                m.mesa_x = Math.Clamp(d.x, 0, PlanoAncho - m.mesa_ancho);
                m.mesa_y = Math.Clamp(d.y, 0, PlanoAlto - m.mesa_alto);
                m.mesa_rotacion = ((d.rot % 360) + 360) % 360;
                m.mesa_capacidad = Math.Clamp(d.cap, 1, 24);
                if (d.nro > 0) m.mesa_nro = d.nro;
                if (!string.IsNullOrWhiteSpace(d.desc)) m.mesa_desc = d.desc.Trim()[..Math.Min(d.desc.Trim().Length, 50)];
            }
            await _context.SaveChangesAsync();
            return Json(new { ok = true, guardadas = existentes.Count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NuevaMesa(string forma, int? zona)
        {
            if (await HttpContext.RequestServices.GetRequiredService<PlanService>().PuedeAgregarAsync(Recurso.Mesa) is string limite)
            {
                TempData["error"] = limite;
                return RedirectToAction(nameof(Index), new { zona });
            }
            forma = FormaMesa.Todas.Contains(forma) ? forma : FormaMesa.Cuadrada;
            var nro = (await _context.Mesas.MaxAsync(m => (int?)m.mesa_nro) ?? 0) + 1;
            _context.Mesas.Add(new MDMesas
            {
                mesa_desc = $"Mesa {nro}",
                mesa_nro = nro,
                mesa_act = true,
                id_zona = await _context.Zonas.AnyAsync(z => z.id_zona == zona) ? zona : null,
                mesa_forma = forma,
                mesa_capacidad = forma == FormaMesa.Rectangular ? 6 : 4,
                mesa_ancho = forma == FormaMesa.Rectangular ? 170 : 100,
                mesa_alto = forma == FormaMesa.Rectangular ? 90 : 100
            });
            await _context.SaveChangesAsync();
            TempData["success"] = $"Mesa {nro} agregada. Arrástrela a su lugar y guarde el plano.";
            return RedirectToAction(nameof(Index), new { zona });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarMesa(int id, int? zona)
        {
            var mesa = await _context.Mesas.FindAsync(id);
            if (mesa != null)
            {
                if (await _context.Cajas.AnyAsync(c => c.id_mesa == id && c.caja_act && !c.caja_pagado))
                {
                    TempData["error"] = $"{mesa.mesa_desc} tiene un pedido abierto; cóbrelo antes de quitarla.";
                }
                else
                {
                    // Se desactiva para conservar el historial de ventas
                    mesa.mesa_act = false;
                    await _context.SaveChangesAsync();
                    TempData["success"] = $"{mesa.mesa_desc} se quitó del plano. Puede reactivarla desde Mesas.";
                }
            }
            return RedirectToAction(nameof(Index), new { zona });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoverMesa(int id, int zona)
        {
            var mesa = await _context.Mesas.FindAsync(id);
            if (mesa != null && await _context.Zonas.AnyAsync(z => z.id_zona == zona && z.zona_act))
            {
                mesa.id_zona = zona;
                mesa.mesa_x = null;
                mesa.mesa_y = null;
                await _context.SaveChangesAsync();
                TempData["success"] = $"{mesa.mesa_desc} se movió de zona.";
            }
            return RedirectToAction(nameof(Index), new { zona });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarZona(int? id, string nombre)
        {
            nombre = nombre?.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
            {
                TempData["error"] = "Ingrese el nombre de la zona.";
                return RedirectToAction(nameof(Index), new { zona = id });
            }
            nombre = nombre[..Math.Min(nombre.Length, 50)];
            MDZona zona = id > 0 ? await _context.Zonas.FindAsync(id) : null;
            if (zona == null)
            {
                zona = new MDZona { zona_orden = (await _context.Zonas.MaxAsync(z => (int?)z.zona_orden) ?? 0) + 1 };
                _context.Zonas.Add(zona);
            }
            zona.zona_desc = nombre;
            zona.zona_act = true;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { zona = zona.id_zona });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarZona(int id)
        {
            var zona = await _context.Zonas.FindAsync(id);
            if (zona == null)
            {
                return RedirectToAction(nameof(Index));
            }
            if (await _context.Mesas.AnyAsync(m => m.id_zona == id && m.mesa_act))
            {
                TempData["error"] = $"La zona {zona.zona_desc} todavía tiene mesas. Muévalas o quítelas antes de eliminarla.";
                return RedirectToAction(nameof(Index), new { zona = id });
            }
            if (await _context.Zonas.CountAsync(z => z.zona_act) <= 1)
            {
                TempData["error"] = "Debe quedar al menos una zona.";
                return RedirectToAction(nameof(Index), new { zona = id });
            }
            zona.zona_act = false;
            await _context.SaveChangesAsync();
            TempData["success"] = $"Zona {zona.zona_desc} eliminada.";
            return RedirectToAction(nameof(Index));
        }

        private const int PlanoAncho = 1200;
        private const int PlanoAlto = 800;
    }
}
