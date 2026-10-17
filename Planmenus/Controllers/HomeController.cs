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

        public async Task<IActionResult> Inicio()
        {
            // Las cifras de venta solo se muestran a quien puede ver el cuadre o la lista de tickets
            if (!User.HasClaim("submenu3_2", "S") && !User.HasClaim("submenu3_3", "S"))
            {
                return View();
            }

            var hoy = Fechas.HoyPeru();
            var manana = hoy.AddDays(1);
            var ayer = hoy.AddDays(-1);
            var cajasHoy = _contex.Cajas.Where(c => c.caja_fecha >= hoy && c.caja_fecha < manana);
            var activasHoy = cajasHoy.Where(c => c.caja_act);

            var dashboard = new VMDashboard
            {
                ventas = await activasHoy.SumAsync(c => (decimal?)c.caja_monto) ?? 0,
                tickets = await activasHoy.CountAsync(),
                anulados = await cajasHoy.CountAsync(c => !c.caja_act),
                ventas_ayer = await _contex.Cajas.Where(c => c.caja_act && c.caja_fecha >= ayer && c.caja_fecha < hoy).SumAsync(c => (decimal?)c.caja_monto) ?? 0,
                platos_programados = await _contex.Menus.CountAsync(m => m.menu_fecha >= hoy && m.menu_fecha < manana),
                top_platos = await (from c in activasHoy
                                    join d in _contex.Caja_Detalles on c.id_caja equals d.id_caja
                                    join m in _contex.Menus on d.id_menu equals m.id_menu
                                    group d by m.menu_desc into g
                                    orderby g.Sum(x => x.det_cant) descending
                                    select new VMDashboardFila { etiqueta = g.Key, cantidad = g.Sum(x => x.det_cant), monto = g.Sum(x => x.det_subtotal) })
                                   .Take(5).ToListAsync(),
                por_tipo_pago = await (from c in activasHoy
                                       join t in _contex.Tipo_Pagos on c.id_tipago equals t.id_tipago
                                       group c by t.tipago_desc into g
                                       orderby g.Sum(x => x.caja_monto) descending
                                       select new VMDashboardFila { etiqueta = g.Key, cantidad = g.Count(), monto = g.Sum(x => x.caja_monto) })
                                      .ToListAsync(),
                por_hora = (await activasHoy
                                .GroupBy(c => c.caja_fecha.Hour)
                                .Select(g => new { hora = g.Key, cantidad = g.Count(), monto = g.Sum(x => x.caja_monto) })
                                .ToListAsync())
                            .OrderBy(x => x.hora)
                            .Select(x => new VMDashboardFila { etiqueta = $"{x.hora:00}:00", cantidad = x.cantidad, monto = x.monto })
                            .ToList()
            };
            return View(dashboard);
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
