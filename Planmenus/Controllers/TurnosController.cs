using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Helpers;
using Planmenus.Services;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Apertura y cierre de caja (turnos) con arqueo de efectivo.
    /// </summary>
    [Permiso("submenu3_10")]
    public class TurnosController : Controller
    {
        private readonly Datacontext _context;
        private readonly TurnoService _turnos;

        public TurnosController(Datacontext context, TurnoService turnos)
        {
            _context = context;
            _turnos = turnos;
        }

        public async Task<IActionResult> Index()
        {
            var abierto = await _turnos.AbiertoAsync();
            ViewBag.resumen = abierto == null ? null : await _turnos.ResumenAsync(abierto);
            ViewBag.historial = await _context.Turnos.AsNoTracking()
                .Where(t => !t.turno_abierto)
                .OrderByDescending(t => t.id_turno)
                .Take(30)
                .ToListAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Abrir(decimal fondo)
        {
            var error = await _turnos.AbrirAsync(fondo, SessionHelper.GetNameIdentifier(User));
            if (error != null) TempData["error"] = error;
            else TempData["success"] = "Caja abierta. ¡Buen turno!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cerrar(int id, decimal contado, string obs)
        {
            var error = await _turnos.CerrarAsync(id, contado, obs, SessionHelper.GetNameIdentifier(User));
            if (error != null)
            {
                TempData["error"] = error;
                return RedirectToAction(nameof(Index));
            }
            TempData["success"] = "Caja cerrada.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        public async Task<IActionResult> Detalle(int id)
        {
            var turno = await _context.Turnos.AsNoTracking().FirstOrDefaultAsync(t => t.id_turno == id);
            if (turno == null)
            {
                return NotFound();
            }
            return View(await _turnos.ResumenAsync(turno));
        }
    }
}
