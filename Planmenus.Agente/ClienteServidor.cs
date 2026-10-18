using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Planmenus.Agente
{
    /// <summary>
    /// Comunicacion con la API /api/agente del sistema web.
    /// </summary>
    public class ClienteServidor
    {
        private readonly HttpClient _http;

        public ClienteServidor(HttpClient http, IOptions<OpcionesAgente> opciones)
        {
            _http = http;
            _http.BaseAddress = new Uri(opciones.Value.Servidor.TrimEnd('/') + "/");
            _http.Timeout = TimeSpan.FromSeconds(20);
            _http.DefaultRequestHeaders.Add("X-Agente-Clave", opciones.Value.Clave);
        }

        public record Trabajo(int id, string impresora, string tipo, string datos);
        public record RespuestaEstado(string? impresora, int modo);

        public async Task<RespuestaEstado?> ReportarEstadoAsync(string equipo, string version, List<string> impresoras, CancellationToken ct)
        {
            var r = await _http.PostAsJsonAsync("api/agente/estado", new { equipo, version, impresoras }, ct);
            await Verificar(r, ct);
            return await r.Content.ReadFromJsonAsync<RespuestaEstado>(cancellationToken: ct);
        }

        public async Task<List<Trabajo>> TrabajosAsync(CancellationToken ct)
        {
            var r = await _http.GetAsync("api/agente/trabajos?max=5", ct);
            await Verificar(r, ct);
            return await r.Content.ReadFromJsonAsync<List<Trabajo>>(cancellationToken: ct) ?? new();
        }

        public async Task InformarAsync(int id, bool ok, string? error, CancellationToken ct)
        {
            var r = await _http.PostAsJsonAsync($"api/agente/trabajos/{id}", new { ok, error }, ct);
            await Verificar(r, ct);
        }

        private static async Task Verificar(HttpResponseMessage r, CancellationToken ct)
        {
            if (r.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new ClaveInvalidaException(await r.Content.ReadAsStringAsync(ct));
            }
            r.EnsureSuccessStatusCode();
        }
    }

    public class ClaveInvalidaException : Exception
    {
        public ClaveInvalidaException(string mensaje) : base(mensaje) { }
    }
}
