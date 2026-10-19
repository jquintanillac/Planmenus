namespace Planmenus.Services.Facturacion
{
    /// <summary>
    /// Cada 10 minutos reintenta enviar a SUNAT los comprobantes que no llegaron (sin internet, SUNAT caido...).
    /// </summary>
    public class FacturacionWorker : BackgroundService
    {
        private static readonly TimeSpan Intervalo = TimeSpan.FromMinutes(10);
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<FacturacionWorker> _logger;

        public FacturacionWorker(IServiceScopeFactory scopes, ILogger<FacturacionWorker> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            using var timer = new PeriodicTimer(Intervalo);
            do
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<EmpresaService>().PorCadaEmpresaAsync(async sp =>
                    {
                        var fe = sp.GetRequiredService<FacturacionService>();
                        foreach (var id in await fe.PorReenviarAsync())
                        {
                            await fe.ReenviarAsync(id);
                        }
                    }, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error al reenviar comprobantes a SUNAT");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
    }
}
