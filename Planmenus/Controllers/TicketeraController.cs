using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Models;
using Planmenus.Services;
using System.Net;
using System.Security.Cryptography;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Configuracion de la ticketera: conexion (navegador, red o agente local), papel, logo,
    /// secciones del ticket, corte, copias y gaveta. Incluye vista previa e impresion de prueba.
    /// </summary>
    [Permiso("submenu1_1")]
    public class TicketeraController : Controller
    {
        private const int LogoAnchoMaximo = 576;   // puntos de una cabeza de 80 mm
        private const int LogoAltoMaximo = 400;

        private readonly Datacontext _context;
        private readonly TicketService _tickets;

        public TicketeraController(Datacontext context, TicketService tickets)
        {
            _context = context;
            _tickets = tickets;
        }

        public async Task<IActionResult> Index()
        {
            var formato = await _tickets.ObtenerFormatoAsync();
            await CargarAsync(formato);
            return View(formato);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(MDTicketera datos)
        {
            Validar(datos);
            if (!ModelState.IsValid)
            {
                var actual = await _tickets.ObtenerFormatoAsync();
                datos.tk_logo_png = actual.tk_logo_png;
                datos.tk_logo_raster = actual.tk_logo_raster;
                await CargarAsync(datos);
                return View(datos);
            }

            var f = await ObtenerParaEditarAsync();
            Copiar(datos, f);
            await _context.SaveChangesAsync();
            TempData["success"] = "Configuración de la ticketera guardada.";
            return RedirectToAction(nameof(Index));
        }

        public class LogoDatos
        {
            public string png { get; set; }
            public string raster { get; set; }
            public int ancho { get; set; }
            public int alto { get; set; }
        }

        /// <summary>
        /// Guarda el logo ya procesado en el navegador: PNG reducido para pantalla/PDF y
        /// mapa de 1 bit por punto para la impresora termica.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(2_000_000)]
        public async Task<IActionResult> Logo([FromBody] LogoDatos d)
        {
            byte[] png, raster;
            try
            {
                png = Convert.FromBase64String(d?.png ?? "");
                raster = Convert.FromBase64String(d?.raster ?? "");
            }
            catch (FormatException)
            {
                return BadRequest("Imagen inválida.");
            }
            bool esPng = png.Length > 8 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47;
            if (!esPng || png.Length > 600_000)
            {
                return BadRequest("El logo debe ser una imagen de hasta 600 KB.");
            }
            if (d.ancho is < 8 or > LogoAnchoMaximo || d.alto is < 8 or > LogoAltoMaximo || raster.Length != (d.ancho + 7) / 8 * d.alto)
            {
                return BadRequest("Tamaño de logo no válido.");
            }

            var f = await ObtenerParaEditarAsync();
            f.tk_logo_png = png;
            f.tk_logo_raster = raster;
            f.tk_logo_ancho = d.ancho;
            f.tk_logo_alto = d.alto;
            f.tk_logo_mostrar = true;
            await _context.SaveChangesAsync();
            return Json(new { ok = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarLogo()
        {
            var f = await ObtenerParaEditarAsync();
            f.tk_logo_png = null;
            f.tk_logo_raster = null;
            f.tk_logo_ancho = 0;
            f.tk_logo_alto = 0;
            await _context.SaveChangesAsync();
            TempData["success"] = "Logo eliminado.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Vista previa en vivo: arma el ticket de ejemplo con los valores del formulario (sin guardar)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vista(MDTicketera datos)
        {
            var actual = await _tickets.ObtenerFormatoAsync();
            var f = new MDTicketera { tk_logo_png = actual.tk_logo_png, tk_logo_raster = actual.tk_logo_raster, tk_logo_ancho = actual.tk_logo_ancho, tk_logo_alto = actual.tk_logo_alto };
            Copiar(datos, f);
            ViewData["mostrar_corte"] = true;
            return PartialView("_TicketPapel", await EjemploAsync(f));
        }

        // GET: Ticketera/Muestra - ticket de ejemplo para imprimir desde el navegador
        public async Task<IActionResult> Muestra()
        {
            ViewBag.imprimir = true;
            return View("~/Views/MDCajas/Ticket.cshtml", await EjemploAsync(await _tickets.ObtenerFormatoAsync()));
        }

        public async Task<IActionResult> MuestraPdf([FromServices] PdfService pdf)
        {
            return File(pdf.Ticket(await EjemploAsync(await _tickets.ObtenerFormatoAsync())), "application/pdf");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Prueba([FromServices] ImpresionService impresion)
        {
            var f = await _tickets.ObtenerFormatoAsync();
            if (f.tk_modo == ModoImpresion.Navegador)
            {
                return RedirectToAction(nameof(Muestra));
            }
            var r = await impresion.ImprimirAsync(await EjemploAsync(f), abrirGaveta: f.tk_gaveta, tipo: "prueba");
            TempData[r.Ok ? "success" : "error"] = r.Mensaje;
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Genera una clave nueva para el agente de impresion; se muestra una sola vez</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarClave()
        {
            var clave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
            var f = await ObtenerParaEditarAsync();
            f.tk_agente_clave_hash = AgenteController.Hash(clave);
            await _context.SaveChangesAsync();
            TempData["clave_agente"] = clave;
            return RedirectToAction(nameof(Index), null, "agente");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reintentar(int id)
        {
            var t = await _context.Impresiones.FindAsync(id);
            if (t != null)
            {
                t.imp_estado = EstadoImpresion.Pendiente;
                t.imp_tomado = null;
                t.imp_intentos = 0;
                t.imp_error = null;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index), null, "agente");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VaciarCola()
        {
            await _context.Impresiones.Where(t => t.imp_estado == EstadoImpresion.Pendiente || t.imp_estado == EstadoImpresion.Error).ExecuteDeleteAsync();
            TempData["success"] = "Se descartaron los trabajos pendientes y con error.";
            return RedirectToAction(nameof(Index), null, "agente");
        }

        private void Validar(MDTicketera d)
        {
            if (d.tk_modo == ModoImpresion.Red && (string.IsNullOrWhiteSpace(d.tk_ip) || !IPAddress.TryParse(d.tk_ip.Trim(), out _)))
            {
                ModelState.AddModelError(nameof(d.tk_ip), "Ingrese la IP de la ticketera, por ejemplo 192.168.1.100.");
            }
            if (d.tk_modo == ModoImpresion.Agente && string.IsNullOrWhiteSpace(d.tk_impresora))
            {
                ModelState.AddModelError(nameof(d.tk_impresora), "Elija o escriba el nombre de la impresora tal como aparece en Windows.");
            }
            if (d.tk_puerto is < 1 or > 65535)
            {
                ModelState.AddModelError(nameof(d.tk_puerto), "El puerto debe estar entre 1 y 65535 (normalmente 9100).");
            }
        }

        /// <summary>Copia los campos editables del formulario (no toca logo ni datos del agente)</summary>
        private static void Copiar(MDTicketera d, MDTicketera f)
        {
            f.tk_modo = d.tk_modo is ModoImpresion.Red or ModoImpresion.Agente ? d.tk_modo : ModoImpresion.Navegador;
            f.tk_impresora = d.tk_impresora?.Trim();
            f.tk_ip = string.IsNullOrWhiteSpace(d.tk_ip) ? null : d.tk_ip.Trim();
            f.tk_puerto = Math.Clamp(d.tk_puerto, 1, 65535);
            f.tk_ancho_mm = d.tk_ancho_mm == 58 ? 58 : 80;
            f.tk_columnas = Math.Clamp(d.tk_columnas, 24, 64);
            f.tk_codepage = EscPosService.PaginasCodigo.ContainsKey(d.tk_codepage) ? d.tk_codepage : 850;
            f.tk_logo_mostrar = d.tk_logo_mostrar;
            f.tk_mostrar_ruc = d.tk_mostrar_ruc;
            f.tk_mostrar_direccion = d.tk_mostrar_direccion;
            f.tk_mostrar_telefono = d.tk_mostrar_telefono;
            f.tk_mostrar_mesa = d.tk_mostrar_mesa;
            f.tk_mostrar_mozo = d.tk_mostrar_mozo;
            f.tk_mostrar_obs = d.tk_mostrar_obs;
            f.tk_total_grande = d.tk_total_grande;
            f.tk_pie = d.tk_pie?.Trim();
            f.tk_corte = d.tk_corte <= TipoCorte.Ninguno ? d.tk_corte : TipoCorte.Parcial;
            f.tk_avance = Math.Clamp(d.tk_avance, 0, 10);
            f.tk_copias = Math.Clamp(d.tk_copias, 1, 5);
            f.tk_gaveta = d.tk_gaveta;
            f.tk_auto = d.tk_auto;
        }

        private async Task<MDTicketera> ObtenerParaEditarAsync()
        {
            var f = await _context.Ticketeras.OrderBy(t => t.id_ticketera).FirstOrDefaultAsync();
            if (f == null)
            {
                f = new MDTicketera();
                _context.Ticketeras.Add(f);
            }
            return f;
        }

        private async Task CargarAsync(MDTicketera f)
        {
            ViewBag.conectado = ImpresionService.AgenteConectado(f);
            ViewBag.impresoras = (f.tk_agente_impresoras ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries);
            ViewBag.cola = await _context.Impresiones.AsNoTracking()
                .OrderByDescending(t => t.id_trabajo).Take(15)
                .Select(t => new MDImpresion_Cola
                {
                    id_trabajo = t.id_trabajo, imp_creado = t.imp_creado, imp_tipo = t.imp_tipo, id_caja = t.id_caja,
                    imp_impresora = t.imp_impresora, imp_estado = t.imp_estado, imp_intentos = t.imp_intentos, imp_error = t.imp_error
                }).ToListAsync();
            ViewBag.url_servidor = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        }

        private async Task<VMTicket> EjemploAsync(MDTicketera f)
        {
            var e = await _tickets.ObtenerEmpresaAsync();
            return new VMTicket
            {
                ticket = 0,
                fecha = Fechas.AhoraPeru(),
                usuario = "Rosa (mozo)",
                tipo_pago = "Efectivo",
                mesa = "Mesa 4",
                total = 47.50m,
                items = new()
                {
                    new VMTicketItem { descripcion = "Lomo saltado", cantidad = 1, precio = 22.00m, subtotal = 22.00m },
                    new VMTicketItem { descripcion = "Ceviche mixto con camote y cancha", cantidad = 1, precio = 16.50m, subtotal = 16.50m },
                    new VMTicketItem { descripcion = "Chicha morada", cantidad = 2, precio = 4.50m, subtotal = 9.00m }
                },
                observacion = "Sin ají en el ceviche",
                empresa = e.gene_empre,
                ruc = e.gene_ruc ?? "20123456789",
                direccion = e.gene_direc ?? "Av. Ejemplo 123",
                telefono = e.gene_telef,
                pie = string.IsNullOrWhiteSpace(f.tk_pie) ? "¡Gracias por su preferencia!" : f.tk_pie,
                ancho_mm = f.tk_ancho_mm == 58 ? 58 : 80,
                formato = f
            };
        }
    }
}
