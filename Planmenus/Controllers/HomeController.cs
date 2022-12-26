using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Planmenus.Data;
using Planmenus.Helpers;
using Planmenus.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

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

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Inicio()
        {
            return View();
        }

        public async Task<IActionResult> login(string Usua_user, string Usua_pass)
        {
            try
            {

                //declaracion de menu y submenu para acceder
                string menu1 = "N";
                string menu2 = "N";
                string menu3 = "N";
              
                //submenu Administracion
                string submenu1_1 = "N";
                string submenu1_2 = "N";
                string submenu1_3 = "N";
                string submenu1_4 = "N";
                string submenu1_5 = "N";
                string submenu1_6 = "N";
                string submenu1_7 = "N";               

                //submenu Planificacion
                string submenu2_1 = "N";
                string submenu2_2 = "N";
                string submenu2_3 = "N";
                string submenu2_4 = "N";
                string submenu2_5 = "N";
                string submenu2_6 = "N";
                string submenu2_7 = "N";
                string submenu2_8 = "N";

                //submenu Caja
                string submenu3_1 = "N";
                string submenu3_2 = "N";
                string submenu3_3 = "N";
                string submenu3_4 = "N";
                string submenu3_5 = "N";
                string submenu3_6 = "N";
                string submenu3_7 = "N";

                var xUsuario = await _contex.Usuarios.Where(x => x.Usua_user == Usua_user).SingleOrDefaultAsync();
                var role_usr = await _contex.Rol_Usuarios.Where(x => x.IdUsuario == xUsuario.IdUsuario).SingleOrDefaultAsync();                
                var rol = await _contex.Rols.Where(x => x.id_rol == role_usr.id_rol).SingleOrDefaultAsync();
                if (xUsuario == null)
                {
                    TempData["error"] = "Usuario/clave incorrectos.";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    //if (HashHelper.CheckHash(Usua_pass, xUsuario.Usua_pass, xUsuario.Usua_Hash))
                    if (xUsuario != null)
                    {
                        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
                        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, xUsuario.Usua_user.ToString()));
                        identity.AddClaim(new Claim(ClaimTypes.Name, xUsuario.Usua_nombres));
                        identity.AddClaim(new Claim(ClaimTypes.Email, xUsuario.Usua_email));
                        identity.AddClaim(new Claim(ClaimTypes.Role, rol.rol_desc));

                        var permisos = await _contex.Rol_Permisos.Where(x => x.id_rol == rol.id_rol).OrderBy(x => x.id_submenu).ToListAsync();

                        foreach (var perm in permisos)
                        {
                            switch (perm.id_menprin)
                            {
                                case 1:
                                    var contmenu1 = _contex.Rol_Permisos.Where(x => x.id_menprin == 1 && x.rolper_act == true && x.id_rol == perm.id_rol).Count();
                                    if (contmenu1 > 0)
                                    {
                                        menu1 = "S";
                                        identity.AddClaim(new Claim("menu1", menu1));
                                    }

                                    if (perm.id_submenu == 1 && perm.rolper_act == true)
                                    {
                                        submenu1_1 = "S";
                                        identity.AddClaim(new Claim("submenu1_1", submenu1_1));
                                    }

                                    if (perm.id_submenu == 2 && perm.rolper_act == true)
                                    {
                                        submenu1_2 = "S";
                                        identity.AddClaim(new Claim("submenu1_2", submenu1_2));
                                    }

                                    if (perm.id_submenu == 3 && perm.rolper_act == true)
                                    {
                                        submenu1_3 = "S";
                                        identity.AddClaim(new Claim("submenu1_3", submenu1_3));
                                    }

                                    if (perm.id_submenu == 4 && perm.rolper_act == true)
                                    {
                                        submenu1_4 = "S";
                                        identity.AddClaim(new Claim("submenu1_4", submenu1_4));
                                    }
                                    if (perm.id_submenu == 5 && perm.rolper_act == true)
                                    {
                                        submenu1_5 = "S";
                                        identity.AddClaim(new Claim("submenu1_5", submenu1_5));
                                    }

                                    if (perm.id_submenu == 6 && perm.rolper_act == true)
                                    {
                                        submenu1_6 = "S";
                                        identity.AddClaim(new Claim("submenu1_6", submenu1_6));
                                    }

                                    if (perm.id_submenu == 7 && perm.rolper_act == true)
                                    {
                                        submenu1_7 = "S";
                                        identity.AddClaim(new Claim("submenu1_7", submenu1_7));
                                    }                           

                                    break;
                                case 2:
                                    var contmenu2 = _contex.Rol_Permisos.Where(x => x.id_menprin == 2 && x.rolper_act == true && x.id_rol == perm.id_rol).Count();
                                    if (contmenu2 > 0)
                                    {
                                        menu2 = "S";
                                        identity.AddClaim(new Claim("menu2", menu2));
                                    }

                                    if (perm.id_submenu == 8 && perm.rolper_act == true)
                                    {
                                        submenu2_1 = "S";
                                        identity.AddClaim(new Claim("submenu2_1", submenu2_1));
                                    }
                                    if (perm.id_submenu == 9 && perm.rolper_act == true)
                                    {
                                        submenu2_2 = "S";
                                        identity.AddClaim(new Claim("submenu2_2", submenu2_2));
                                    }

                                    if (perm.id_submenu == 10 && perm.rolper_act == true)
                                    {
                                        submenu2_3 = "S";
                                        identity.AddClaim(new Claim("submenu2_3", submenu2_3));
                                    }

                                    if (perm.id_submenu == 11 && perm.rolper_act == true)
                                    {
                                        submenu2_4 = "S";
                                        identity.AddClaim(new Claim("submenu2_4", submenu2_4));
                                    }
                                    if (perm.id_submenu == 12 && perm.rolper_act == true)
                                    {
                                        submenu2_5 = "S";
                                        identity.AddClaim(new Claim("submenu2_5", submenu2_5));
                                    }
                                    if (perm.id_submenu == 13 && perm.rolper_act == true)
                                    {
                                        submenu2_6 = "S";
                                        identity.AddClaim(new Claim("submenu2_6", submenu2_6));
                                    }
                                    if (perm.id_submenu == 14 && perm.rolper_act == true)
                                    {
                                        submenu2_7 = "S";
                                        identity.AddClaim(new Claim("submenu2_7", submenu2_7));
                                    }
                                    if (perm.id_submenu == 15 && perm.rolper_act == true)
                                    {
                                        submenu2_8 = "S";
                                        identity.AddClaim(new Claim("submenu2_8", submenu2_8));
                                    }

                                    break;
                                case 3:
                                    var contmenu3 = _contex.Rol_Permisos.Where(x => x.id_menprin == 3 && x.rolper_act == true && x.id_rol == perm.id_rol).Count();
                                    if (contmenu3 > 0)
                                    {
                                        menu3 = "S";
                                        identity.AddClaim(new Claim("menu3", menu3));
                                    }

                                    if (perm.id_submenu == 18 && perm.rolper_act == true)
                                    {
                                        submenu3_1 = "S";
                                        identity.AddClaim(new Claim("submenu3_1", submenu3_1));
                                    }
                                    if (perm.id_submenu == 19 && perm.rolper_act == true)
                                    {
                                        submenu3_2 = "S";
                                        identity.AddClaim(new Claim("submenu3_2", submenu3_2));
                                    }
                                    if (perm.id_submenu == 19 && perm.rolper_act == true)
                                    {
                                        submenu3_3 = "S";
                                        identity.AddClaim(new Claim("submenu3_3", submenu3_3));
                                    }
                                    if (perm.id_submenu == 19 && perm.rolper_act == true)
                                    {
                                        submenu3_4 = "S";
                                        identity.AddClaim(new Claim("submenu3_4", submenu3_4));
                                    }
                                    if (perm.id_submenu == 19 && perm.rolper_act == true)
                                    {
                                        submenu3_5 = "S";
                                        identity.AddClaim(new Claim("submenu3_5", submenu3_5));
                                    }
                                    if (perm.id_submenu == 19 && perm.rolper_act == true)
                                    {
                                        submenu3_6 = "S";
                                        identity.AddClaim(new Claim("submenu3_6", submenu3_6));
                                    }
                                    if (perm.id_submenu == 19 && perm.rolper_act == true)
                                    {
                                        submenu3_7 = "S";
                                        identity.AddClaim(new Claim("submenu3_7", submenu3_7));
                                    }
                                    break;                            
                            }
                        }
                        var principal = new ClaimsPrincipal(identity);
                        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
                        new AuthenticationProperties { ExpiresUtc = DateTime.Now.AddDays(2), IsPersistent = true });
                        return RedirectToAction("Index", "MDMenus");
                    }
                    else
                    {
                        TempData["error"] = "usuario/clave incorrecta.";
                        return RedirectToAction("Index", "Home");
                    }
                }
            }
            catch (System.Exception)
            {
                TempData["error"] = "Usuario/clave incorrectos.";
                return RedirectToAction("Index", "Home");
            }         
            
        }

        public IActionResult Privacy()
        {
            return View();
        }

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