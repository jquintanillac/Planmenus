using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Helpers;
using Planmenus.Services;
using QRCoder;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Carta digital: pagina publica con el menu del dia y su codigo QR para las mesas.
    /// </summary>
    public class CartaController : Controller
    {
        private readonly Datacontext _context;
        private readonly TicketService _tickets;

        public CartaController(Datacontext context, TicketService tickets)
        {
            _context = context;
            _tickets = tickets;
        }

        // GET: /c/{codigo} - publica, la abre el cliente al escanear el QR de su restaurante.
        // /Carta sin codigo sigue funcionando cuando el sistema tiene una sola empresa (QR antiguos).
        [AllowAnonymous]
        [Route("c/{codigo}")]
        [Route("Carta")]
        [Route("Carta/Index")]
        public async Task<IActionResult> Index(string codigo)
        {
            var actual = HttpContext.RequestServices.GetRequiredService<EmpresaActual>();
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                var emp = await _context.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.emp_codigo == codigo.ToLower() && e.emp_activa);
                if (emp == null) return NotFound();
                actual.Establecer(emp.id_empresa);
            }
            else if (actual.Id == null)
            {
                var activas = await _context.Empresas.AsNoTracking().Where(e => e.emp_activa).Select(e => e.id_empresa).Take(2).ToListAsync();
                if (activas.Count != 1) return NotFound();
                actual.Establecer(activas[0]);
            }
            var hoy = Fechas.HoyPeru();
            ViewBag.empresa = await _tickets.ObtenerEmpresaAsync();
            ViewBag.fecha = hoy;
            var menus = await _context.Menus.AsNoTracking()
                .Where(m => m.menu_fecha.Date == hoy)
                .OrderBy(m => m.menu_desc)
                .ToListAsync();
            return View(menus);
        }

        // GET: /Carta/Qr - pantalla interna para imprimir el QR
        [Permiso("submenu2_9")]
        public IActionResult Qr()
        {
            ViewBag.url = UrlCarta();
            return View();
        }

        [Permiso("submenu2_9")]
        public IActionResult QrImagen()
        {
            using var generador = new QRCodeGenerator();
            using var datos = generador.CreateQrCode(UrlCarta(), QRCodeGenerator.ECCLevel.Q);
            var png = new PngByteQRCode(datos).GetGraphic(12);
            return File(png, "image/png");
        }

        /// <summary>Direccion publica de la carta de la empresa actual: /c/{codigo}</summary>
        private string UrlCarta()
        {
            var id = HttpContext.RequestServices.GetRequiredService<EmpresaActual>().Id;
            var codigo = _context.Empresas.Where(e => e.id_empresa == id).Select(e => e.emp_codigo).FirstOrDefault();
            return $"{Request.Scheme}://{Request.Host}{Request.PathBase}/c/{codigo}";
        }
    }
}
