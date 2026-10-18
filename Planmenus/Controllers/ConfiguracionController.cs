using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Models;
using Planmenus.Services;
using System.Net;
using System.Net.Sockets;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Configuracion del negocio: datos de la empresa, formato del ticket y ticketera.
    /// </summary>
    [Permiso("submenu1_1")]
    public class ConfiguracionController : Controller
    {
        private readonly Datacontext _context;

        public ConfiguracionController(Datacontext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var config = await _context.Generales.OrderBy(g => g.id_gene).FirstOrDefaultAsync()
                         ?? new MDGenerales { gene_ticket_ancho = 80, gene_impresora_puerto = 9100 };
            ViewBag.tipos_pago = await _context.Tipo_Pagos.OrderBy(t => t.tipago_desc).ToListAsync();
            ViewBag.url_carta = Url.Action("Index", "Carta", null, Request.Scheme);
            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("gene_empre,gene_ruc,gene_direc,gene_telef,gene_url,gene_ticket_ancho,gene_ticket_pie,gene_impresora_ip,gene_impresora_puerto")] MDGenerales datos, int[] efectivo)
        {
            if (string.IsNullOrWhiteSpace(datos.gene_empre))
            {
                ModelState.AddModelError(nameof(datos.gene_empre), "Ingrese el nombre del negocio.");
            }
            if (!string.IsNullOrWhiteSpace(datos.gene_ruc) && (datos.gene_ruc.Length != 11 || !datos.gene_ruc.All(char.IsDigit)))
            {
                ModelState.AddModelError(nameof(datos.gene_ruc), "El RUC debe tener 11 dígitos.");
            }
            if (!string.IsNullOrWhiteSpace(datos.gene_impresora_ip) && !IPAddress.TryParse(datos.gene_impresora_ip.Trim(), out _))
            {
                ModelState.AddModelError(nameof(datos.gene_impresora_ip), "Ingrese una IP válida, por ejemplo 192.168.1.100.");
            }
            if (datos.gene_impresora_puerto is < 1 or > 65535)
            {
                ModelState.AddModelError(nameof(datos.gene_impresora_puerto), "El puerto debe estar entre 1 y 65535 (normalmente 9100).");
            }
            if (!ModelState.IsValid)
            {
                ViewBag.tipos_pago = await _context.Tipo_Pagos.OrderBy(t => t.tipago_desc).ToListAsync();
                ViewBag.url_carta = Url.Action("Index", "Carta", null, Request.Scheme);
                return View(datos);
            }

            var config = await _context.Generales.OrderBy(g => g.id_gene).FirstOrDefaultAsync();
            if (config == null)
            {
                config = new MDGenerales();
                _context.Generales.Add(config);
            }
            config.gene_empre = datos.gene_empre.Trim();
            config.gene_ruc = datos.gene_ruc?.Trim();
            config.gene_direc = datos.gene_direc?.Trim();
            config.gene_telef = datos.gene_telef?.Trim();
            config.gene_url = datos.gene_url?.Trim();
            config.gene_ticket_ancho = datos.gene_ticket_ancho == 58 ? 58 : 80;
            config.gene_ticket_pie = datos.gene_ticket_pie?.Trim();
            config.gene_impresora_ip = string.IsNullOrWhiteSpace(datos.gene_impresora_ip) ? null : datos.gene_impresora_ip.Trim();
            config.gene_impresora_puerto = datos.gene_impresora_puerto;

            foreach (var tp in await _context.Tipo_Pagos.ToListAsync())
            {
                tp.tipago_efectivo = efectivo?.Contains(tp.id_tipago) == true;
            }

            await _context.SaveChangesAsync();
            TempData["success"] = "Configuración guardada.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Configuracion/VistaPrevia - ticket de ejemplo en PDF con la configuracion actual
        public async Task<IActionResult> VistaPrevia([FromServices] TicketService tickets, [FromServices] PdfService pdf)
        {
            return File(pdf.Ticket(await EjemploAsync(tickets)), "application/pdf");
        }

        // POST: Configuracion/ProbarTicketera - imprime un ticket de prueba por red
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProbarTicketera([FromServices] TicketService tickets, [FromServices] EscPosService escpos, [FromServices] ILogger<ConfiguracionController> logger)
        {
            var empresa = await tickets.ObtenerEmpresaAsync();
            if (string.IsNullOrWhiteSpace(empresa.gene_impresora_ip))
            {
                TempData["error"] = "Primero ingrese y guarde la IP de la ticketera.";
                return RedirectToAction(nameof(Index));
            }
            try
            {
                await escpos.ImprimirAsync(await EjemploAsync(tickets), empresa.gene_impresora_ip, empresa.gene_impresora_puerto);
                TempData["success"] = $"Ticket de prueba enviado a {empresa.gene_impresora_ip}:{empresa.gene_impresora_puerto}.";
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException or IOException)
            {
                logger.LogWarning(ex, "Prueba de ticketera fallida");
                TempData["error"] = $"No se pudo conectar con {empresa.gene_impresora_ip}:{empresa.gene_impresora_puerto}. Revise que la ticketera esté encendida, en la misma red y con esa IP.";
            }
            return RedirectToAction(nameof(Index));
        }

        private static async Task<VMTicket> EjemploAsync(TicketService tickets)
        {
            var e = await tickets.ObtenerEmpresaAsync();
            return new VMTicket
            {
                ticket = 0,
                fecha = Fechas.AhoraPeru(),
                usuario = "prueba",
                tipo_pago = "Efectivo",
                mesa = "Mesa 1",
                total = 27.50m,
                items = new()
                {
                    new VMTicketItem { descripcion = "Lomo saltado", cantidad = 1, precio = 18.00m, subtotal = 18.00m },
                    new VMTicketItem { descripcion = "Chicha morada", cantidad = 2, precio = 4.75m, subtotal = 9.50m }
                },
                observacion = "TICKET DE PRUEBA",
                empresa = e.gene_empre,
                ruc = e.gene_ruc,
                direccion = e.gene_direc,
                telefono = e.gene_telef,
                pie = string.IsNullOrWhiteSpace(e.gene_ticket_pie) ? "¡Gracias por su preferencia!" : e.gene_ticket_pie,
                ancho_mm = e.gene_ticket_ancho == 58 ? 58 : 80
            };
        }
    }
}
