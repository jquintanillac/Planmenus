using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using System.Security.Claims;

namespace Planmenus.Services
{
    /// <summary>
    /// Inicia la sesion de un usuario ya validado: empresa, rol, permisos del menu y cookie.
    /// Lo usan el login y el registro de empresas.
    /// </summary>
    public class SesionService
    {
        private readonly Datacontext _context;
        private readonly EmpresaActual _empresa;

        public SesionService(Datacontext context, EmpresaActual empresa)
        {
            _context = context;
            _empresa = empresa;
        }

        /// <summary>Devuelve null si inicio la sesion o el mensaje de por que no</summary>
        public async Task<string> IniciarAsync(HttpContext http, MDUsuarios usuario)
        {
            _empresa.Establecer(usuario.id_empresa);
            var empresa = await _context.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.id_empresa == usuario.id_empresa);
            if (empresa == null || !empresa.emp_activa)
            {
                return "La empresa de este usuario está suspendida. Comuníquese con soporte.";
            }

            var rol = await (from ru in _context.Rol_Usuarios
                             join r in _context.Rols on ru.id_rol equals r.id_rol
                             where ru.IdUsuario == usuario.IdUsuario && r.rol_act
                             select r).FirstOrDefaultAsync();
            if (rol == null)
            {
                return "El usuario no tiene un rol activo asignado.";
            }

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, usuario.Usua_user));
            identity.AddClaim(new Claim(ClaimTypes.Name, usuario.Usua_nombres ?? usuario.Usua_user));
            identity.AddClaim(new Claim(ClaimTypes.Email, usuario.Usua_email ?? ""));
            identity.AddClaim(new Claim(ClaimTypes.Role, rol.rol_desc));
            identity.AddClaim(new Claim(SessionHelper.ClaimEmpresa, usuario.id_empresa.ToString()));
            if (usuario.Usua_superadmin)
            {
                identity.AddClaim(new Claim(SessionHelper.ClaimSuperadmin, "S"));
            }

            var permisos = await _context.Rol_Permisos.Where(x => x.id_rol == rol.id_rol && x.rolper_act).ToListAsync();
            foreach (var claim in Permisos.ClaimsDe(permisos))
            {
                identity.AddClaim(new Claim(claim, "S"));
            }

            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
                new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddDays(2), IsPersistent = true });

            // Ultimo ingreso (para el panel de la plataforma)
            await _context.Usuarios.Where(u => u.IdUsuario == usuario.IdUsuario)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.Usua_ultimo_ingreso, Fechas.AhoraPeru()));
            return null;
        }
    }
}
