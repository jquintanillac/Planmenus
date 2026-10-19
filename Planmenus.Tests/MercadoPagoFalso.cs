using System.Collections.Concurrent;
using Planmenus.Services.Pagos;

namespace Planmenus.Tests
{
    /// <summary>Mercado Pago de mentira: guarda las preferencias creadas y responde los pagos que la prueba registre</summary>
    public class MercadoPagoFalso : IMercadoPago
    {
        public const string ClaveWebhook = "clave-webhook-pruebas";
        private long _siguiente = 9_000_000_000;

        public bool Configurado => true;
        public ConcurrentBag<Preferencia> Preferencias { get; } = new();
        public ConcurrentDictionary<string, PagoMP> Pagos { get; } = new();

        public Task<string> CrearPreferenciaAsync(Preferencia p, CancellationToken ct = default)
        {
            Preferencias.Add(p);
            return Task.FromResult("https://www.mercadopago.com.pe/checkout/v1/redirect?pref_id=prueba-" + p.ReferenciaExterna);
        }

        public Task<PagoMP> ObtenerPagoAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Pagos.TryGetValue(id, out var p) ? p : null);

        /// <summary>Simula que el cliente pago en Mercado Pago y devuelve el id del pago</summary>
        public string Pagar(string referencia, decimal monto, string estado = "approved", string moneda = "PEN")
        {
            var id = Interlocked.Increment(ref _siguiente);
            Pagos[id.ToString()] = new PagoMP { Id = id, Estado = estado, ReferenciaExterna = referencia, Monto = monto, Moneda = moneda, Medio = "yape" };
            return id.ToString();
        }
    }
}
