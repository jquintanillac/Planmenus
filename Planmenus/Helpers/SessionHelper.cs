using System.Security.Claims;
using System.Security.Principal;

namespace Planmenus.Helpers
{
    public class SessionHelper
    {
        /// <summary>Claim con el id de la empresa del usuario</summary>
        public const string ClaimEmpresa = "empresa";
        /// <summary>Claim de los administradores de la plataforma</summary>
        public const string ClaimSuperadmin = "superadmin";

        public static string GetValue(IPrincipal User, string Property)
        {
            var r = ((ClaimsIdentity)User.Identity).FindFirst(Property);
            return r == null ? "" : r.Value;
        }

        public static string GetNameIdentifier(IPrincipal User)
        {
            var r = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.NameIdentifier);
            return r == null ? "" : r.Value;
        }

        public static string GetName(IPrincipal User)
        {
            var r = ((ClaimsIdentity)User.Identity).FindFirst(ClaimTypes.Name);
            return r == null ? "" : r.Value;
        }
    }
      
}
