using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Services;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Planificacion de mozos: que mozo atiende cada mesa en una fecha.
    /// </summary>
    [Permiso("submenu3_12")]
    public class MozosController : Controller
    {
        private readonly Datacontext _context;
        private readonly PlanoService _plano;

        public MozosController(Datacontext context, PlanoService plano)
        {
            _context = context;
            _plano = plano;
        }

        public async Task<IActionResult> Index(DateTime? fecha)
        {
            var dia = (fecha ?? Fechas.HoyPeru()).Date;
            ViewBag.fecha = dia;
            ViewBag.plano = await _plano.ObtenerAsync(null, conEstado: false);
            ViewBag.mozos = await _plano.MozosAsync();
            ViewBag.asignaciones = await _context.Asignaciones.AsNoTracking()
                .Where(a => a.asig_fecha == dia)
                .ToDictionaryAsync(a => a.id_mesa, a => a.IdUsuario);
            ViewBag.hay_anterior = await _context.Asignaciones.AnyAsync(a => a.asig_fecha < dia);
            return View();
        }

        /// <summary>asig[id_mesa] = IdUsuario (vacio = sin mozo)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(DateTime fecha, Dictionary<int, int?> asig)
        {
            var dia = fecha.Date;
            var mozos = (await _plano.MozosAsync()).Select(m => m.IdUsuario).ToHashSet();
            var mesas = await _context.Mesas.Where(m => m.mesa_act).Select(m => m.id_mesa).ToListAsync();
            var actuales = await _context.Asignaciones.Where(a => a.asig_fecha == dia).ToListAsync();

            foreach (var id_mesa in mesas)
            {
                int? usuario = asig != null && asig.TryGetValue(id_mesa, out var u) && u != null && mozos.Contains(u.Value) ? u : null;
                var actual = actuales.FirstOrDefault(a => a.id_mesa == id_mesa);
                if (usuario == null && actual != null)
                {
                    _context.Asignaciones.Remove(actual);
                }
                else if (usuario != null && actual == null)
                {
                    _context.Asignaciones.Add(new MDAsignacion_Mozo { asig_fecha = dia, id_mesa = id_mesa, IdUsuario = usuario.Value });
                }
                else if (usuario != null)
                {
                    actual.IdUsuario = usuario.Value;
                }
            }
            await _context.SaveChangesAsync();
            TempData["success"] = $"Planificación del {dia:dd/MM/yyyy} guardada.";
            return RedirectToAction(nameof(Index), new { fecha = dia.ToString("yyyy-MM-dd") });
        }

        /// <summary>Copia la ultima planificacion registrada antes de la fecha</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CopiarAnterior(DateTime fecha)
        {
            var dia = fecha.Date;
            var origen = await _context.Asignaciones.Where(a => a.asig_fecha < dia).MaxAsync(a => (DateTime?)a.asig_fecha);
            if (origen == null)
            {
                TempData["error"] = "No hay una planificación anterior para copiar.";
                return RedirectToAction(nameof(Index), new { fecha = dia.ToString("yyyy-MM-dd") });
            }
            _context.Asignaciones.RemoveRange(_context.Asignaciones.Where(a => a.asig_fecha == dia));
            var copia = await _context.Asignaciones.AsNoTracking().Where(a => a.asig_fecha == origen).ToListAsync();
            _context.Asignaciones.AddRange(copia.Select(a => new MDAsignacion_Mozo { asig_fecha = dia, id_mesa = a.id_mesa, IdUsuario = a.IdUsuario }));
            await _context.SaveChangesAsync();
            TempData["success"] = $"Se copió la planificación del {origen:dd/MM/yyyy}. Revise y guarde si hace cambios.";
            return RedirectToAction(nameof(Index), new { fecha = dia.ToString("yyyy-MM-dd") });
        }
    }
}
