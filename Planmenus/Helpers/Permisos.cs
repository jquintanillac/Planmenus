using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Planmenus.Data.Entities;

namespace Planmenus.Helpers
{
    /// <summary>
    /// Traduce los permisos de Rol_Permisos a los claims que usa el menu lateral (_Layout).
    /// </summary>
    public static class Permisos
    {
        // id_menprin -> claim del menu principal
        private static readonly Dictionary<int, string> Menus = new()
        {
            [1] = "menu1", // Administracion
            [2] = "menu2", // Planificacion
            [3] = "menu3", // Caja
        };

        // id_submenu -> claim del submenu
        private static readonly Dictionary<int, string> Submenus = new()
        {
            [1] = "submenu1_1", [2] = "submenu1_2", [3] = "submenu1_3", [4] = "submenu1_4",
            [5] = "submenu1_5", [6] = "submenu1_6", [7] = "submenu1_7",
            [8] = "submenu2_1", [9] = "submenu2_2", [10] = "submenu2_3", [11] = "submenu2_4",
            [12] = "submenu2_5", [13] = "submenu2_6", [14] = "submenu2_7", [15] = "submenu2_8",
            [18] = "submenu3_1", [19] = "submenu3_2", [20] = "submenu3_3", [21] = "submenu3_4",
            [22] = "submenu3_5", [23] = "submenu3_6", [24] = "submenu3_7",
        };

        public static IEnumerable<string> ClaimsDe(IEnumerable<MDRol_Permisos> permisosActivos)
        {
            var claims = new HashSet<string>();
            foreach (var perm in permisosActivos)
            {
                if (Menus.TryGetValue(perm.id_menprin, out var menu))
                {
                    claims.Add(menu);
                }
                if (Submenus.TryGetValue(perm.id_submenu, out var submenu))
                {
                    claims.Add(submenu);
                }
            }
            return claims;
        }
    }

    /// <summary>
    /// Exige que el usuario tenga al menos uno de los permisos (claims de submenu) indicados.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class PermisoAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _claims;

        public PermisoAttribute(params string[] claims)
        {
            _claims = claims;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;
            if (user.Identity?.IsAuthenticated != true)
            {
                context.Result = new ChallengeResult();
                return;
            }

            if (!_claims.Any(c => user.HasClaim(c, "S")))
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
