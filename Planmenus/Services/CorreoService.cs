using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;

namespace Planmenus.Services
{
    /// <summary>
    /// Datos del servidor de correo (seccion "Correo" de appsettings / variables de entorno).
    /// Puerto 587 con STARTTLS (UsarSsl = false) o 465 con SSL (UsarSsl = true).
    /// </summary>
    public class CorreoOptions
    {
        public string Host { get; set; }
        public int Puerto { get; set; } = 587;
        public bool UsarSsl { get; set; }
        public string Usuario { get; set; }
        public string Clave { get; set; }
        public string Remitente { get; set; }
        public string NombreRemitente { get; set; }

        public bool Configurado => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(Remitente);
    }

    /// <summary>
    /// Envia correos por SMTP y deja registro de cada envio en Correo_Log.
    /// </summary>
    public class CorreoService
    {
        private readonly CorreoOptions _opciones;
        private readonly Datacontext _context;
        private readonly ILogger<CorreoService> _logger;

        public CorreoService(IOptions<CorreoOptions> opciones, Datacontext context, ILogger<CorreoService> logger)
        {
            _opciones = opciones.Value;
            _context = context;
            _logger = logger;
        }

        public CorreoOptions Opciones => _opciones;

        /// <summary>
        /// Envia el correo. Devuelve null si salio bien o el mensaje de error.
        /// </summary>
        public async Task<string> EnviarAsync(string tipo, IEnumerable<string> para, string asunto, string html, int? id_reserva = null, CancellationToken ct = default)
        {
            var destinos = Correos.Normalizar(para);
            string error = null;
            if (!_opciones.Configurado)
            {
                error = "El correo no está configurado (sección Correo de appsettings).";
            }
            else if (destinos.Count == 0)
            {
                error = "No hay destinatarios con correo válido.";
            }
            else
            {
                try
                {
                    var mensaje = new MimeMessage();
                    mensaje.From.Add(new MailboxAddress(_opciones.NombreRemitente ?? "", _opciones.Remitente));
                    foreach (var d in destinos)
                    {
                        mensaje.To.Add(MailboxAddress.Parse(d));
                    }
                    mensaje.Subject = asunto;
                    mensaje.Body = new BodyBuilder { HtmlBody = html }.ToMessageBody();

                    using var smtp = new SmtpClient();
                    smtp.Timeout = 20000;
                    var seguridad = _opciones.UsarSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
                    await smtp.ConnectAsync(_opciones.Host, _opciones.Puerto, seguridad, ct);
                    if (!string.IsNullOrWhiteSpace(_opciones.Usuario))
                    {
                        await smtp.AuthenticateAsync(_opciones.Usuario, _opciones.Clave ?? "", ct);
                    }
                    await smtp.SendAsync(mensaje, ct);
                    await smtp.DisconnectAsync(true, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "No se pudo enviar el correo {Tipo} a {Para}", tipo, string.Join(",", destinos));
                    error = ex.Message;
                }
            }

            _context.Correos.Add(new MDCorreo_Log
            {
                cor_fecha = Fechas.AhoraPeru(),
                cor_tipo = tipo,
                id_reserva = id_reserva,
                cor_para = Recortar(string.Join(", ", destinos), 1000),
                cor_asunto = Recortar(asunto, 200),
                cor_ok = error == null,
                cor_error = Recortar(error, 1000)
            });
            await _context.SaveChangesAsync(ct);
            return error;
        }

        private static string Recortar(string s, int max) => s == null || s.Length <= max ? s : s[..max];
    }

    public static class Correos
    {
        /// <summary>Separa listas por coma o punto y coma, quita duplicados y descarta correos invalidos</summary>
        public static List<string> Normalizar(IEnumerable<string> listas) =>
            (listas ?? Enumerable.Empty<string>())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .SelectMany(l => l.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(c => MailboxAddress.TryParse(c, out var m) && m.Address.Contains('@'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
    }
}
