using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Planmenus.Services.Pagos
{
    /// <summary>
    /// Seccion "MercadoPago" (las claves van en user-secrets o variables de entorno, nunca en appsettings.json).
    /// </summary>
    public class MercadoPagoOptions
    {
        /// <summary>true = se usan las credenciales de prueba (Sandbox*); false = produccion, cobros reales</summary>
        public bool Sandbox { get; set; }
        /// <summary>Access Token de produccion (APP_USR-...)</summary>
        public string AccessToken { get; set; }
        /// <summary>Clave publica de produccion (solo la usa un checkout dentro de la pagina; Checkout Pro no la necesita)</summary>
        public string PublicKey { get; set; }
        /// <summary>Access Token de prueba (TEST-...)</summary>
        public string SandboxAccessToken { get; set; }
        public string SandboxPublicKey { get; set; }
        /// <summary>Clave secreta de las notificaciones (Webhooks) para validar la firma x-signature</summary>
        public string WebhookSecret { get; set; }
        /// <summary>Direccion publica del sistema (https://...) para recibir las notificaciones. Vacio = no se piden.</summary>
        public string UrlPublica { get; set; }

        /// <summary>El token que corresponde al modo actual</summary>
        public string Token => Sandbox ? SandboxAccessToken : AccessToken;

        /// <summary>Hay un token real (no vacio ni el texto de ejemplo "REEMPLAZA")</summary>
        public bool Configurado => !string.IsNullOrWhiteSpace(Token) && !Token.Contains("REEMPLAZA", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Lo que el sistema necesita de Mercado Pago (se reemplaza en las pruebas)</summary>
    public interface IMercadoPago
    {
        bool Configurado { get; }
        /// <summary>Crea la preferencia de Checkout Pro y devuelve la direccion a la que se envia al cliente</summary>
        Task<string> CrearPreferenciaAsync(Preferencia p, CancellationToken ct = default);
        /// <summary>Consulta un pago por su id (la fuente de verdad: nunca se confia en lo que llega en la notificacion)</summary>
        Task<PagoMP> ObtenerPagoAsync(string id, CancellationToken ct = default);
    }

    public record Preferencia(string Titulo, decimal Monto, string ReferenciaExterna, string Email, string UrlRetorno, string UrlNotificacion);

    public class PagoMP
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("status")] public string Estado { get; set; }
        [JsonPropertyName("status_detail")] public string Detalle { get; set; }
        [JsonPropertyName("external_reference")] public string ReferenciaExterna { get; set; }
        [JsonPropertyName("transaction_amount")] public decimal Monto { get; set; }
        [JsonPropertyName("currency_id")] public string Moneda { get; set; }
        [JsonPropertyName("payment_method_id")] public string Medio { get; set; }
        [JsonPropertyName("live_mode")] public bool Produccion { get; set; }

        public bool Aprobado => Estado == "approved";
    }

    public class MercadoPagoClient : IMercadoPago
    {
        private readonly HttpClient _http;
        private readonly MercadoPagoOptions _opciones;

        public MercadoPagoClient(HttpClient http, IOptions<MercadoPagoOptions> opciones)
        {
            _http = http;
            _opciones = opciones.Value;
            _http.BaseAddress = new Uri("https://api.mercadopago.com/");
            _http.Timeout = TimeSpan.FromSeconds(30);
        }

        public bool Configurado => _opciones.Configurado;

        public async Task<string> CrearPreferenciaAsync(Preferencia p, CancellationToken ct = default)
        {
            var cuerpo = new Dictionary<string, object>
            {
                ["items"] = new[] { new { id = "suscripcion", title = p.Titulo, quantity = 1, unit_price = p.Monto, currency_id = "PEN" } },
                ["external_reference"] = p.ReferenciaExterna,
                ["back_urls"] = new { success = p.UrlRetorno, pending = p.UrlRetorno, failure = p.UrlRetorno },
                ["statement_descriptor"] = "PLANMENUS",
            };
            if (!string.IsNullOrWhiteSpace(p.Email)) cuerpo["payer"] = new { email = p.Email };
            // Mercado Pago solo vuelve solo al sitio y notifica a direcciones publicas https
            if (p.UrlRetorno.StartsWith("https://") && !p.UrlRetorno.Contains("localhost")) cuerpo["auto_return"] = "approved";
            if (!string.IsNullOrWhiteSpace(p.UrlNotificacion)) cuerpo["notification_url"] = p.UrlNotificacion;

            using var req = Solicitud(HttpMethod.Post, "checkout/preferences");
            req.Headers.Add("X-Idempotency-Key", p.ReferenciaExterna);
            req.Content = JsonContent.Create(cuerpo);
            using var r = await _http.SendAsync(req, ct);
            var texto = await r.Content.ReadAsStringAsync(ct);
            if (!r.IsSuccessStatusCode) throw new InvalidOperationException($"Mercado Pago respondió {(int)r.StatusCode}: {Recortar(texto)}");
            using var json = JsonDocument.Parse(texto);
            return json.RootElement.GetProperty("init_point").GetString();
        }

        public async Task<PagoMP> ObtenerPagoAsync(string id, CancellationToken ct = default)
        {
            if (!long.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out _)) return null;
            using var req = Solicitud(HttpMethod.Get, "v1/payments/" + id);
            using var r = await _http.SendAsync(req, ct);
            if (r.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            if (!r.IsSuccessStatusCode) throw new InvalidOperationException($"Mercado Pago respondió {(int)r.StatusCode} al consultar el pago {id}.");
            return await r.Content.ReadFromJsonAsync<PagoMP>(cancellationToken: ct);
        }

        private HttpRequestMessage Solicitud(HttpMethod metodo, string ruta)
        {
            if (!Configurado) throw new InvalidOperationException("Mercado Pago no está configurado (MercadoPago:AccessToken o, en modo Sandbox, MercadoPago:SandboxAccessToken).");
            var req = new HttpRequestMessage(metodo, ruta);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opciones.Token);
            return req;
        }

        private static string Recortar(string s) => s.Length > 300 ? s[..300] : s;

        /// <summary>
        /// Valida la cabecera x-signature de una notificacion: "ts=...,v1=..." donde v1 es
        /// HMAC-SHA256(clave, "id:{data.id};request-id:{x-request-id};ts:{ts};").
        /// </summary>
        public static bool FirmaValida(string firma, string requestId, string dataId, string clave)
        {
            if (string.IsNullOrEmpty(firma) || string.IsNullOrEmpty(clave)) return false;
            string ts = null, v1 = null;
            foreach (var parte in firma.Split(','))
            {
                var kv = parte.Split('=', 2);
                if (kv.Length != 2) continue;
                if (kv[0].Trim() == "ts") ts = kv[1].Trim();
                if (kv[0].Trim() == "v1") v1 = kv[1].Trim();
            }
            if (ts == null || v1 == null) return false;
            var manifiesto = new StringBuilder();
            if (!string.IsNullOrEmpty(dataId)) manifiesto.Append("id:").Append(dataId.ToLowerInvariant()).Append(';');
            if (!string.IsNullOrEmpty(requestId)) manifiesto.Append("request-id:").Append(requestId).Append(';');
            manifiesto.Append("ts:").Append(ts).Append(';');
            var calculada = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(clave), Encoding.UTF8.GetBytes(manifiesto.ToString()))).ToLowerInvariant();
            return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(calculada), Encoding.ASCII.GetBytes(v1.ToLowerInvariant()));
        }
    }
}
