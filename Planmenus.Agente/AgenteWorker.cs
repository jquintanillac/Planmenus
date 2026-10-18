using Microsoft.Extensions.Options;

namespace Planmenus.Agente
{
    /// <summary>
    /// Cada pocos segundos pide al sistema los tickets pendientes y los imprime.
    /// Cada 30 segundos informa que esta conectado y que impresoras tiene el equipo.
    /// </summary>
    public class AgenteWorker : BackgroundService
    {
        private static readonly TimeSpan IntervaloEstado = TimeSpan.FromSeconds(30);
        private readonly ClienteServidor _servidor;
        private readonly OpcionesAgente _opciones;
        private readonly ILogger<AgenteWorker> _logger;
        private readonly string _version = typeof(AgenteWorker).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

        public AgenteWorker(ClienteServidor servidor, IOptions<OpcionesAgente> opciones, ILogger<AgenteWorker> logger)
        {
            _servidor = servidor;
            _opciones = opciones.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_opciones.Clave) || _opciones.Servidor.Contains("ejemplo.com"))
            {
                _logger.LogError("Configure 'Servidor' y 'Clave' en appsettings.json (sección Agente). La clave se genera en Configuración → Ticketera.");
                return;
            }
            _logger.LogInformation("Agente de impresión {Version} conectando con {Servidor}", _version, _opciones.Servidor);

            var ultimoEstado = DateTime.MinValue;
            var espera = TimeSpan.FromSeconds(Math.Clamp(_opciones.IntervaloSegundos, 1, 60));
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (DateTime.UtcNow - ultimoEstado > IntervaloEstado)
                    {
                        var impresoras = ImpresoraWindows.Instaladas();
                        var r = await _servidor.ReportarEstadoAsync(Environment.MachineName, _version, impresoras, ct);
                        if (ultimoEstado == DateTime.MinValue)
                        {
                            _logger.LogInformation("Conectado. Impresoras en este equipo: {Impresoras}. Impresora elegida en el sistema: {Elegida}",
                                string.Join(", ", impresoras), r?.impresora ?? "(ninguna)");
                        }
                        ultimoEstado = DateTime.UtcNow;
                    }

                    foreach (var t in await _servidor.TrabajosAsync(ct))
                    {
                        await ImprimirAsync(t, ct);
                    }
                }
                catch (ClaveInvalidaException ex)
                {
                    _logger.LogError("El sistema rechazó la clave del agente: {Mensaje}", ex.Message);
                    await Task.Delay(TimeSpan.FromMinutes(1), ct);
                }
                catch (Exception ex) when ((ex is HttpRequestException || ex is TaskCanceledException) && !ct.IsCancellationRequested)
                {
                    _logger.LogWarning("Sin conexión con el sistema ({Mensaje}). Reintentando…", ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(15), ct);
                }
                await Task.Delay(espera, ct);
            }
        }

        private async Task ImprimirAsync(ClienteServidor.Trabajo t, CancellationToken ct)
        {
            try
            {
                var datos = Convert.FromBase64String(t.datos);
                if (!string.IsNullOrWhiteSpace(_opciones.CarpetaPrueba))
                {
                    Directory.CreateDirectory(_opciones.CarpetaPrueba);
                    var archivo = Path.Combine(_opciones.CarpetaPrueba, $"trabajo-{t.id}-{t.tipo}.bin");
                    await File.WriteAllBytesAsync(archivo, datos, ct);
                    _logger.LogInformation("Trabajo {Id} guardado en {Archivo} (modo prueba)", t.id, archivo);
                }
                else
                {
                    ImpresoraWindows.ImprimirRaw(t.impresora, datos, $"Planmenus {t.tipo} {t.id}");
                    _logger.LogInformation("Trabajo {Id} ({Tipo}) impreso en {Impresora}", t.id, t.tipo, t.impresora);
                }
                await _servidor.InformarAsync(t.id, true, null, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning("No se pudo imprimir el trabajo {Id} en {Impresora}: {Mensaje}", t.id, t.impresora, ex.Message);
                await _servidor.InformarAsync(t.id, false, ex.Message, ct);
            }
        }
    }
}
