using Microsoft.AspNetCore.Mvc;
using Planmenus.Helpers;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Los datos generales ahora se editan en Configuracion; se mantiene la ruta por compatibilidad.
    /// </summary>
    [Permiso("submenu1_1")]
    public class MDGeneralesController : Controller
    {
        public IActionResult Index() => RedirectToAction("Index", "Configuracion");
    }
}
