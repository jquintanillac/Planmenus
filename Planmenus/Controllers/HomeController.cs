using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Models;
using System.Diagnostics;
using System.Security.Claims;

namespace Planmenus.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly Datacontext _contex;
        public HomeController(ILogger<HomeController> logger, Datacontext contex)
        {
            _logger = logger;
            _contex = contex;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Inicio()
        {
            return View();
        }

        public IActionResult AccesoDenegado()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> login(string Usua_user, string Usua_pass)
        {
            try
            {
                var xUsuario = await _contex.Usuarios.SingleOrDefaultAsync(x => x.Usua_user == Usua_user && x.Usua_act);
                if (xUsuario == null || !await ValidarPassword(xUsuario, Usua_pass))
                {
                    TempData["error"] = "Usuario/clave incorrectos.";
                    return RedirectToAction("Index", "Home");
                }

                var rol = await (from ru in _contex.Rol_Usuarios
                                 join r in _contex.Rols on ru.id_rol equals r.id_rol
                                 where ru.IdUsuario == xUsuario.IdUsuario && r.rol_act
                                 select r).FirstOrDefaultAsync();
                if (rol == null)
                {
                    TempData["error"] = "El usuario no tiene un rol activo asignado.";
                    return RedirectToAction("Index", "Home");
                }

                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, xUsuario.Usua_user));
                identity.AddClaim(new Claim(ClaimTypes.Name, xUsuario.Usua_nombres ?? xUsuario.Usua_user));
                identity.AddClaim(new Claim(ClaimTypes.Email, xUsuario.Usua_email ?? ""));
                identity.AddClaim(new Claim(ClaimTypes.Role, rol.rol_desc));

                var permisos = await _contex.Rol_Permisos
                    .Where(x => x.id_rol == rol.id_rol && x.rolper_act)
                    .ToListAsync();

                foreach (var claim in Permisos.ClaimsDe(permisos))
                {
                    identity.AddClaim(new Claim(claim, "S"));
                }

                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddDays(2), IsPersistent = true });
                return RedirectToAction("Inicio", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al iniciar sesion del usuario {Usuario}", Usua_user);
                TempData["error"] = "No se pudo iniciar sesión. Intente nuevamente.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Valida la contraseña. Si el usuario aun tiene la contraseña antigua en texto plano
        /// y coincide, se reemplaza por su hash en ese momento.
        /// </summary>
        private async Task<bool> ValidarPassword(MDUsuarios usuario, string password)
        {
            if (HashHelper.CheckHash(password, usuario.Usua_pass, usuario.Usua_Hash))
            {
                return true;
            }

            if (!HashHelper.EsHash(usuario.Usua_pass) && !string.IsNullOrEmpty(password) && usuario.Usua_pass == password)
            {
                var hash = HashHelper.Hash(password);
                usuario.Usua_pass = hash.Password;
                usuario.Usua_Hash = hash.Salt;
                await _contex.SaveChangesAsync();
                return true;
            }

            return false;
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

    }
}
