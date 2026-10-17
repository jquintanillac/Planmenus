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

        // GET: /Carta - publica, la abre el cliente al escanear el QR
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
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

        private string UrlCarta() => Url.Action(nameof(Index), "Carta", null, Request.Scheme);
    }
}
