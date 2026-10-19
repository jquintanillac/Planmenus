namespace Planmenus.Services
{
    /// <summary>
    /// Revisa cada 5 minutos las reservas confirmadas y envia el recordatorio "su reserva comienza pronto".
    /// En hosting compartido la aplicacion debe mantenerse activa (sin suspension por inactividad)
    /// para que este proceso siga corriendo.
    /// </summary>
    public class RecordatorioWorker : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(5);
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<RecordatorioWorker> _logger;

        public RecordatorioWorker(IServiceScopeFactory scopes, ILogger<RecordatorioWorker> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Pequena espera para no competir con el arranque de la aplicacion
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            using var timer = new PeriodicTimer(Intervalo);
            do
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    // Cada empresa con sus reservas, su configuracion y sus correos
                    await scope.ServiceProvider.GetRequiredService<EmpresaService>().PorCadaEmpresaAsync(async sp =>
                    {
                        var enviados = await sp.GetRequiredService<ReservaService>().EnviarRecordatoriosAsync(stoppingToken);
                        if (enviados > 0)
                        {
                            _logger.LogInformation("Recordatorios de reserva procesados: {Cantidad}", enviados);
                        }
                    }, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error al enviar recordatorios de reserva");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
