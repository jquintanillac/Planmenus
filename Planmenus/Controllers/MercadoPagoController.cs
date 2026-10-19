using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Planmenus.Services.Pagos;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Notificaciones (Webhooks) de Mercado Pago. Se valida la firma y el pago se consulta
    /// siempre a Mercado Pago: lo que llega en la notificacion solo indica que pago revisar.
    /// </summary>
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public class MercadoPagoController : Controller
    {
        private readonly CobroService _cobro;
        private readonly MercadoPagoOptions _opciones;
        private readonly ILogger<MercadoPagoController> _logger;

        public MercadoPagoController(CobroService cobro, IOptions<MercadoPagoOptions> opciones, ILogger<MercadoPagoController> logger)
        {
            _cobro = cobro;
            _opciones = opciones.Value;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Webhook()
        {
            // Formato actual: ?data.id=123&type=payment (y el mismo dato en el cuerpo); formato antiguo: ?id=123&topic=payment
            string tipo = Request.Query["type"].FirstOrDefault() ?? Request.Query["topic"].FirstOrDefault();
            string id = Request.Query["data.id"].FirstOrDefault() ?? Request.Query["id"].FirstOrDefault();
            if (id == null || tipo == null)
            {
                try
                {
                    using var json = await JsonDocument.ParseAsync(Request.Body);
                    var raiz = json.RootElement;
                    if (tipo == null && raiz.TryGetProperty("type", out var t)) tipo = t.GetString();
                    if (id == null && raiz.TryGetProperty("data", out var d) && d.TryGetProperty("id", out var i)) id = i.ToString();
                }
                catch (JsonException) { }
            }

            if (!string.IsNullOrEmpty(_opciones.WebhookSecret)
                && !MercadoPagoClient.FirmaValida(Request.Headers["x-signature"], Request.Headers["x-request-id"], Request.Query["data.id"].FirstOrDefault() ?? id, _opciones.WebhookSecret))
            {
                _logger.LogWarning("Notificación de Mercado Pago con firma no válida (pago {Pago})", id);
                return Unauthorized();
            }
            if (tipo != "payment" || string.IsNullOrEmpty(id))
            {
                return Ok();
            }

            try
            {
                var (resultado, empresa, _) = await _cobro.ProcesarAsync(id);
                _logger.LogInformation("Notificación de Mercado Pago: pago {Pago} de la empresa {Empresa}: {Resultado}", id, empresa, resultado);
                return Ok();
            }
            catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException)
            {
                // Mercado Pago reintenta la notificacion si no respondemos 200
                _logger.LogError(ex, "No se pudo procesar la notificación del pago {Pago}", id);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }
    }
}
