using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Planmenus.Data;

namespace Planmenus.Services.Pagos
{
    /// <summary>
    /// Cobro de la suscripcion con Mercado Pago (Checkout Pro): arma el pago del plan elegido y,
    /// cuando Mercado Pago lo aprueba, extiende la suscripcion de la empresa.
    /// </summary>
    public class CobroService
    {
        public static readonly int[] MesesPermitidos = { 1, 3, 6, 12 };
        private const string Prefijo = "pm";

        private readonly IMercadoPago _mp;
        private readonly EmpresaService _empresas;
        private readonly EmpresaActual _empresa;
        private readonly SuscripcionOptions _suscripcion;
        private readonly MercadoPagoOptions _opciones;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CobroService> _logger;

        public CobroService(IMercadoPago mp, EmpresaService empresas, EmpresaActual empresa, IOptions<SuscripcionOptions> suscripcion,
                            IOptions<MercadoPagoOptions> opciones, IMemoryCache cache, ILogger<CobroService> logger)
        {
            _mp = mp;
            _empresas = empresas;
            _empresa = empresa;
            _suscripcion = suscripcion.Value;
            _opciones = opciones.Value;
            _cache = cache;
            _logger = logger;
        }

        public bool Disponible => _mp.Configurado;

        /// <summary>Precio total de un plan por varios meses</summary>
        public static decimal Total(Plan p, int meses) => Math.Round(p.PrecioMes * meses, 2);

        /// <summary>Crea el pago en Mercado Pago para la empresa actual y devuelve la direccion de pago</summary>
        public async Task<string> IniciarAsync(string plan, int meses, string email, string urlRetorno, string nombreEmpresa)
        {
            var p = _suscripcion.Todos.FirstOrDefault(x => x.Codigo == plan) ?? throw new ArgumentException("Plan no válido.");
            if (!MesesPermitidos.Contains(meses)) throw new ArgumentException("Cantidad de meses no válida.");
            var referencia = string.Join(':', Prefijo, _empresa.Id, p.Codigo, meses, Guid.NewGuid().ToString("N")[..10]);
            var notificacion = string.IsNullOrWhiteSpace(_opciones.UrlPublica) ? null : _opciones.UrlPublica.TrimEnd('/') + "/MercadoPago/Webhook";
            var titulo = $"Planmenus {p.Nombre} · {meses} {(meses == 1 ? "mes" : "meses")} · {nombreEmpresa}";
            return await _mp.CrearPreferenciaAsync(new Preferencia(titulo, Total(p, meses), referencia, email, urlRetorno, notificacion));
        }

        public enum Resultado { Aplicado, YaAplicado, Pendiente, Rechazado, Invalido }

        /// <summary>
        /// Consulta el pago en Mercado Pago y, si esta aprobado y coincide con lo que se cobro,
        /// lo aplica a la empresa de su referencia. Se puede llamar varias veces con el mismo pago.
        /// </summary>
        public async Task<(Resultado Resultado, int? Empresa, DateTime? Vence)> ProcesarAsync(string idPago)
        {
            var pago = await _mp.ObtenerPagoAsync(idPago);
            if (pago == null) return (Resultado.Invalido, null, null);

            // Referencia "pm:{empresa}:{plan}:{meses}:{unico}" creada por IniciarAsync
            var partes = (pago.ReferenciaExterna ?? "").Split(':');
            if (partes.Length != 5 || partes[0] != Prefijo || !int.TryParse(partes[1], out var idEmpresa) || !int.TryParse(partes[3], out var meses))
            {
                _logger.LogWarning("Pago {Pago} de Mercado Pago con referencia desconocida: {Referencia}", pago.Id, pago.ReferenciaExterna);
                return (Resultado.Invalido, null, null);
            }
            if (pago.Estado is "pending" or "in_process" or "authorized") return (Resultado.Pendiente, idEmpresa, null);
            if (!pago.Aprobado) return (Resultado.Rechazado, idEmpresa, null);

            var plan = _suscripcion.Todos.FirstOrDefault(x => x.Codigo == partes[2]);
            if (plan == null || !MesesPermitidos.Contains(meses) || pago.Moneda != "PEN" || pago.Monto < Total(plan, meses))
            {
                _logger.LogWarning("Pago {Pago} aprobado que no coincide con el plan: {Monto} {Moneda} para {Referencia}", pago.Id, pago.Monto, pago.Moneda, pago.ReferenciaExterna);
                return (Resultado.Invalido, idEmpresa, null);
            }

            var vence = await _empresas.RegistrarPagoAsync(idEmpresa, new EmpresaService.Pago(plan.Codigo, meses, pago.Monto,
                "Mercado Pago", $"{pago.Medio} · {pago.Id.ToString(CultureInfo.InvariantCulture)}", "mercadopago", pago.Id.ToString(CultureInfo.InvariantCulture)));
            _cache.Remove(EstadoEmpresa.Clave(idEmpresa));
            if (vence == null) return (Resultado.YaAplicado, idEmpresa, null);
            _logger.LogInformation("Pago {Pago} de Mercado Pago aplicado a la empresa {Empresa}: plan {Plan} hasta {Vence:d}", pago.Id, idEmpresa, plan.Codigo, vence);
            return (Resultado.Aplicado, idEmpresa, vence);
        }
    }
}
