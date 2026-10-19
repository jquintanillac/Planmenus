using System.Collections.Concurrent;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Planmenus.Data;
using Planmenus.Services;

namespace Planmenus.Tests
{
    /// <summary>Bandeja de los correos "enviados" durante las pruebas</summary>
    public class Buzon
    {
        public ConcurrentQueue<MimeMessage> Mensajes { get; } = new();

        public List<MimeMessage> Para(string correo) =>
            Mensajes.Where(m => m.To.Mailboxes.Any(x => string.Equals(x.Address, correo, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    /// <summary>Correo que no sale a internet: deja el mensaje en el Buzon</summary>
    public class CorreoFalso : CorreoService
    {
        private readonly Buzon _buzon;

        public CorreoFalso(IOptions<CorreoOptions> opciones, Datacontext context, ILogger<CorreoService> logger, Buzon buzon)
            : base(opciones, context, logger)
        {
            _buzon = buzon;
        }

        protected override Task TransmitirAsync(MimeMessage mensaje, CancellationToken ct)
        {
            _buzon.Mensajes.Enqueue(mensaje);
            return Task.CompletedTask;
        }
    }

    /// <summary>SUNAT no disponible: los comprobantes se numeran y quedan para reenviar, sin salir a la red</summary>
    public class SunatSinRed : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) { Content = new StringContent("sin red en pruebas") });
    }
}
