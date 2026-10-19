using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Services;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Configuracion del negocio: datos de la empresa, pagos en efectivo, correo y carta digital.
    /// La ticketera tiene su propia pantalla (TicketeraController).
    /// </summary>
    [Permiso("submenu1_1")]
    public class ConfiguracionController : Controller
    {
        private readonly Datacontext _context;
        private readonly CorreoService _correo;

        public ConfiguracionController(Datacontext context, CorreoService correo)
        {
            _context = context;
            _correo = correo;
        }

        public async Task<IActionResult> Index()
        {
            var config = await _context.Generales.OrderBy(g => g.id_gene).FirstOrDefaultAsync() ?? new MDGenerales();
            await CargarAsync();
            return View(config);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index([Bind("gene_empre,gene_ruc,gene_direc,gene_telef,gene_url,gene_correo_avisos,gene_recordatorio_horas,gene_reserva_minutos")] MDGenerales datos, int[] efectivo)
        {
            if (string.IsNullOrWhiteSpace(datos.gene_empre))
            {
                ModelState.AddModelError(nameof(datos.gene_empre), "Ingrese el nombre del negocio.");
            }
            if (!string.IsNullOrWhiteSpace(datos.gene_ruc) && (datos.gene_ruc.Trim().Length != 11 || !datos.gene_ruc.Trim().All(char.IsDigit)))
            {
                ModelState.AddModelError(nameof(datos.gene_ruc), "El RUC debe tener 11 dígitos.");
            }
            if (!ModelState.IsValid)
            {
                await CargarAsync();
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
            config.gene_correo_avisos = string.Join(", ", Correos.Normalizar(new[] { datos.gene_correo_avisos }));
            config.gene_recordatorio_horas = Math.Clamp(datos.gene_recordatorio_horas, 1, 72);
            config.gene_reserva_minutos = Math.Clamp(datos.gene_reserva_minutos, 30, 720);

            foreach (var tp in await _context.Tipo_Pagos.ToListAsync())
            {
                tp.tipago_efectivo = efectivo?.Contains(tp.id_tipago) == true;
            }

            await _context.SaveChangesAsync();
            TempData["success"] = "Configuración guardada.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Configuracion/ProbarCorreo - envia un correo de prueba con la configuracion de appsettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProbarCorreo(string destino, [FromServices] TicketService tickets)
        {
            var empresa = await tickets.ObtenerEmpresaAsync();
            var error = await _correo.EnviarAsync("prueba", new[] { destino }, $"Correo de prueba · {empresa.gene_empre}",
                $"<p>¡Hola! Este es un correo de prueba de <strong>{System.Net.WebUtility.HtmlEncode(empresa.gene_empre)}</strong>.</p><p>Si lo recibió, los avisos de reservas funcionarán correctamente.</p>");
            if (error == null) TempData["success"] = $"Correo de prueba enviado a {destino}.";
            else TempData["error"] = "No se pudo enviar el correo de prueba: " + error;
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarAsync()
        {
            ViewBag.tipos_pago = await _context.Tipo_Pagos.OrderBy(t => t.tipago_desc).ToListAsync();
            ViewBag.url_carta = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/c/" + await _context.Empresas.Where(e => e.id_empresa == HttpContext.RequestServices.GetRequiredService<EmpresaActual>().Id).Select(e => e.emp_codigo).FirstOrDefaultAsync();
            ViewBag.correo = _correo.Opciones;
            ViewBag.formato = await HttpContext.RequestServices.GetRequiredService<TicketService>().ObtenerFormatoAsync();
            ViewBag.fe = await HttpContext.RequestServices.GetRequiredService<Planmenus.Services.Facturacion.FacturacionService>().ConfigAsync();
        }
    }
}
