using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Helpers;
using Planmenus.Services;
using Planmenus.Services.Pagos;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Mi suscripcion: plan actual, uso frente a los limites, vencimiento, planes disponibles y pago con Mercado Pago.
    /// Es la unica pantalla disponible cuando la suscripcion vencio.
    /// </summary>
    public class SuscripcionController : Controller
    {
        private readonly Datacontext _context;
        private readonly PlanService _planes;
        private readonly CobroService _cobro;
        private readonly EmpresaActual _empresa;
        private readonly ILogger<SuscripcionController> _logger;

        public SuscripcionController(Datacontext context, PlanService planes, CobroService cobro, EmpresaActual empresa, ILogger<SuscripcionController> logger)
        {
            _context = context;
            _planes = planes;
            _cobro = cobro;
            _empresa = empresa;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.opciones = _planes.Opciones;
            ViewBag.pagos = await _context.Pagos_Suscripcion.AsNoTracking().OrderByDescending(p => p.id_pago).Take(12).ToListAsync();
            ViewBag.mercadopago = _cobro.Disponible;
            ViewBag.puede_pagar = User.HasClaim("submenu1_1", "S");
            return View(await _planes.EstadoAsync());
        }

        // POST: Suscripcion/Pagar - lleva al checkout de Mercado Pago
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("submenu1_1")]
        public async Task<IActionResult> Pagar(string plan, int meses)
        {
            if (!_cobro.Disponible)
            {
                TempData["error"] = "El pago en línea todavía no está disponible. Use las instrucciones de pago.";
                return RedirectToAction(nameof(Index));
            }
            try
            {
                var empresa = await _context.Empresas.AsNoTracking().FirstAsync(e => e.id_empresa == _empresa.Id);
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var url = await _cobro.IniciarAsync(plan, meses, email, Url.Action(nameof(Retorno), "Suscripcion", null, Request.Scheme), empresa.emp_nombre);
                return Redirect(url);
            }
            catch (ArgumentException ex)
            {
                TempData["error"] = ex.Message;
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "No se pudo crear el pago en Mercado Pago");
                TempData["error"] = "No se pudo conectar con Mercado Pago. Intente nuevamente en unos minutos.";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Suscripcion/Retorno?payment_id=...&status=... - Mercado Pago devuelve aqui al cliente
        public async Task<IActionResult> Retorno(string payment_id, string collection_id, string status)
        {
            var id = string.IsNullOrEmpty(payment_id) || payment_id == "null" ? collection_id : payment_id;
            if (string.IsNullOrEmpty(id) || id == "null")
            {
                TempData["error"] = "No se completó el pago.";
                return RedirectToAction(nameof(Index));
            }
            try
            {
                // Se aplica aqui tambien por si la notificacion de Mercado Pago no llega (por ejemplo, en pruebas locales)
                var (resultado, empresa, vence) = await _cobro.ProcesarAsync(id);
                if (empresa != null && empresa != _empresa.Id) resultado = CobroService.Resultado.Invalido;
                switch (resultado)
                {
                    case CobroService.Resultado.Aplicado:
                        TempData["success"] = $"¡Gracias! Pago aprobado. Su suscripción está vigente hasta el {vence:dd/MM/yyyy}.";
                        break;
                    case CobroService.Resultado.YaAplicado:
                        TempData["success"] = "¡Gracias! Su pago ya está registrado.";
                        break;
                    case CobroService.Resultado.Pendiente:
                        TempData["success"] = "Su pago está en proceso. Cuando Mercado Pago lo apruebe, la suscripción se renueva sola.";
                        break;
                    case CobroService.Resultado.Rechazado:
                        TempData["error"] = "Mercado Pago rechazó el pago. Puede intentar con otro medio.";
                        break;
                    default:
                        TempData["error"] = "No se pudo verificar el pago. Si se le cobró, escríbanos con el número de operación.";
                        break;
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                _logger.LogError(ex, "No se pudo verificar el pago {Pago} de Mercado Pago", id);
                TempData["error"] = "No se pudo verificar el pago con Mercado Pago. Si se aprobó, se aplicará en unos minutos.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
