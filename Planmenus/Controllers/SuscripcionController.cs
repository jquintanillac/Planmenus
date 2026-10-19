using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Services;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Mi suscripcion: plan actual, uso frente a los limites, vencimiento, planes disponibles y como pagar.
    /// Es la unica pantalla disponible cuando la suscripcion vencio.
    /// </summary>
    public class SuscripcionController : Controller
    {
        private readonly Datacontext _context;
        private readonly PlanService _planes;

        public SuscripcionController(Datacontext context, PlanService planes)
        {
            _context = context;
            _planes = planes;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.opciones = _planes.Opciones;
            ViewBag.pagos = await _context.Pagos_Suscripcion.AsNoTracking().OrderByDescending(p => p.id_pago).Take(12).ToListAsync();
            return View(await _planes.EstadoAsync());
        }
    }
}
