using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Services;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Recuperar la contraseña por correo: se envia un enlace que vence en 1 hora y sirve una sola vez
    /// (deja de valer en cuanto la contraseña cambia).
    /// </summary>
    [AllowAnonymous]
    public class RecuperarController : Controller
    {
        public const string Politica = "recuperar";
        public const string Respuesta = "Si el usuario o correo está registrado y tiene correo, le enviamos un enlace para crear una nueva contraseña. Revise también su carpeta de spam.";
        private static readonly TimeSpan Vigencia = TimeSpan.FromHours(1);

        private readonly Datacontext _context;
        private readonly ITimeLimitedDataProtector _protector;
        private readonly ILogger<RecuperarController> _logger;

        public RecuperarController(Datacontext context, IDataProtectionProvider proteccion, ILogger<RecuperarController> logger)
        {
            _context = context;
            _protector = proteccion.CreateProtector("Planmenus.Recuperar").ToTimeLimitedDataProtector();
            _logger = logger;
        }

        public IActionResult Index() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(Politica)]
        public async Task<IActionResult> Index(string dato)
        {
            dato = dato?.Trim();
            if (string.IsNullOrEmpty(dato))
            {
                ModelState.AddModelError("", "Ingrese su usuario o su correo.");
                return View();
            }

            var usuarios = await _context.Usuarios.IgnoreQueryFilters().AsNoTracking()
                .Where(u => u.Usua_act && u.Usua_email != null && u.Usua_email != "" && (u.Usua_user == dato || u.Usua_email == dato))
                .Take(5).ToListAsync();
            var empresa = HttpContext.RequestServices.GetRequiredService<EmpresaActual>();
            var correo = HttpContext.RequestServices.GetRequiredService<CorreoService>();
            foreach (var u in usuarios)
            {
                // El registro del correo enviado pertenece a la empresa del usuario
                empresa.Establecer(u.id_empresa);
                var enlace = Url.Action(nameof(Nueva), "Recuperar", new { t = _protector.Protect($"{u.IdUsuario}|{Huella(u)}", Vigencia) }, Request.Scheme);
                var error = await correo.EnviarAsync("recuperar", new[] { u.Usua_email }, "Cree una nueva contraseña", Html(u, enlace));
                if (error != null) _logger.LogWarning("No se pudo enviar la recuperación al usuario {Usuario}: {Error}", u.Usua_user, error);
            }
            // La misma respuesta exista o no el usuario: no revela quien esta registrado
            TempData["success"] = Respuesta;
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Nueva(string t)
        {
            if (await UsuarioDelEnlaceAsync(t) == null) return EnlaceInvalido();
            ViewBag.t = t;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(Politica)]
        public async Task<IActionResult> Nueva(string t, string clave, string clave2)
        {
            var usuario = await UsuarioDelEnlaceAsync(t);
            if (usuario == null) return EnlaceInvalido();
            if (string.IsNullOrEmpty(clave) || clave.Length < 8) ModelState.AddModelError("", "La contraseña debe tener al menos 8 caracteres.");
            else if (clave != clave2) ModelState.AddModelError("", "Las contraseñas no coinciden.");
            if (!ModelState.IsValid)
            {
                ViewBag.t = t;
                return View();
            }

            var hash = HashHelper.Hash(clave);
            await _context.Usuarios.IgnoreQueryFilters().Where(u => u.IdUsuario == usuario.IdUsuario)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Usua_pass, hash.Password).SetProperty(u => u.Usua_Hash, hash.Salt));
            TempData["success"] = $"Listo, su contraseña cambió. Ingrese con su usuario {usuario.Usua_user}.";
            return RedirectToAction("Index", "Home");
        }

        private IActionResult EnlaceInvalido()
        {
            TempData["error"] = "El enlace venció o ya se usó. Pida uno nuevo.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Usuario del enlace si es valido, no vencio y la contraseña no cambio desde que se envio</summary>
        private async Task<MDUsuarios> UsuarioDelEnlaceAsync(string t)
        {
            if (string.IsNullOrEmpty(t)) return null;
            string datos;
            try { datos = _protector.Unprotect(t); }
            catch (CryptographicException) { return null; }
            var partes = datos.Split('|');
            if (partes.Length != 2 || !int.TryParse(partes[0], out var id)) return null;
            var u = await _context.Usuarios.IgnoreQueryFilters().AsNoTracking().FirstOrDefaultAsync(x => x.IdUsuario == id && x.Usua_act);
            return u != null && Huella(u) == partes[1] ? u : null;
        }

        /// <summary>Resumen de la contraseña actual: cambia cuando se cambia la contraseña</summary>
        public static string Huella(MDUsuarios u) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes((u.Usua_pass ?? "") + "|" + (u.Usua_Hash ?? ""))))[..16];

        private static string Html(MDUsuarios u, string enlace)
        {
            var nombre = WebUtility.HtmlEncode(u.Usua_nombres ?? u.Usua_user);
            var url = WebUtility.HtmlEncode(enlace);
            return $@"<div style=""font-family:Arial,sans-serif;max-width:520px;margin:auto;color:#1f1a17"">
<h2 style=""margin:0 0 12px"">Hola, {nombre}</h2>
<p>Recibimos un pedido para cambiar la contraseña del usuario <strong>{WebUtility.HtmlEncode(u.Usua_user)}</strong>.</p>
<p style=""margin:24px 0""><a href=""{url}"" style=""background:#e4572e;color:#fff;padding:12px 20px;border-radius:10px;text-decoration:none;font-weight:bold"">Crear nueva contraseña</a></p>
<p style=""color:#7a6e64;font-size:13px"">El enlace vence en 1 hora y sirve una sola vez. Si usted no lo pidió, ignore este correo: su contraseña no cambia.</p>
</div>";
        }
    }
}
