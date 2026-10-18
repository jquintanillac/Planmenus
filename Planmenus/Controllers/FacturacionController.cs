using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Services.Facturacion;
using System.Text.RegularExpressions;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Configuracion de la facturacion electronica: activacion, emisor, credenciales SOL,
    /// certificado digital, series e IGV.
    /// </summary>
    [Permiso("submenu1_1")]
    public class FacturacionController : Controller
    {
        private readonly Datacontext _context;
        private readonly FacturacionService _fe;

        public FacturacionController(Datacontext context, FacturacionService fe)
        {
            _context = context;
            _fe = fe;
        }

        public async Task<IActionResult> Index()
        {
            var fe = await _fe.ConfigAsync();
            if (fe.id_fe == 0)
            {
                // Se proponen los datos del negocio ya registrados en Configuracion
                var g = await _context.Generales.OrderBy(x => x.id_gene).FirstOrDefaultAsync();
                fe.fe_ruc = g?.gene_ruc;
                fe.fe_razon_social = g?.gene_empre;
                fe.fe_direccion = g?.gene_direc;
            }
            await CargarAsync(fe);
            return View(fe);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(MDFacturacion d, string clave_sol)
        {
            var serie = new Regex("^[A-Z0-9]{3}$");
            if (!string.IsNullOrWhiteSpace(d.fe_ruc) && !Documentos.RucValido(d.fe_ruc)) ModelState.AddModelError(nameof(d.fe_ruc), "El RUC no es válido.");
            if (!string.IsNullOrWhiteSpace(d.fe_ubigeo) && !Regex.IsMatch(d.fe_ubigeo, "^[0-9]{6}$")) ModelState.AddModelError(nameof(d.fe_ubigeo), "El ubigeo tiene 6 dígitos (ej.: 150101 Lima).");
            ValidarSerie(d.fe_serie_boleta, 'B', nameof(d.fe_serie_boleta), serie);
            ValidarSerie(d.fe_serie_factura, 'F', nameof(d.fe_serie_factura), serie);
            ValidarSerie(d.fe_serie_nc_boleta, 'B', nameof(d.fe_serie_nc_boleta), serie);
            ValidarSerie(d.fe_serie_nc_factura, 'F', nameof(d.fe_serie_nc_factura), serie);
            if (d.fe_igv is < 0 or > 30) ModelState.AddModelError(nameof(d.fe_igv), "Ingrese la tasa en porcentaje, por ejemplo 18.");

            var fe = await _context.Facturacion.OrderBy(x => x.id_fe).FirstOrDefaultAsync();
            if (fe == null)
            {
                fe = new MDFacturacion();
                _context.Facturacion.Add(fe);
            }
            if (!ModelState.IsValid)
            {
                d.fe_certificado = fe.fe_certificado;
                d.fe_certificado_titular = fe.fe_certificado_titular;
                d.fe_certificado_vence = fe.fe_certificado_vence;
                d.fe_clave_sol = fe.fe_clave_sol;
                await CargarAsync(d);
                return View(d);
            }

            fe.fe_ambiente = d.fe_ambiente == AmbienteFe.Produccion ? AmbienteFe.Produccion : AmbienteFe.Pruebas;
            fe.fe_ruc = d.fe_ruc?.Trim();
            fe.fe_razon_social = d.fe_razon_social?.Trim().ToUpperInvariant();
            fe.fe_nombre_comercial = d.fe_nombre_comercial?.Trim();
            fe.fe_direccion = d.fe_direccion?.Trim().ToUpperInvariant();
            fe.fe_ubigeo = d.fe_ubigeo?.Trim();
            fe.fe_establecimiento = string.IsNullOrWhiteSpace(d.fe_establecimiento) ? "0000" : d.fe_establecimiento.Trim();
            fe.fe_usuario_sol = d.fe_usuario_sol?.Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(clave_sol)) fe.fe_clave_sol = _fe.Cifrar(clave_sol);
            fe.fe_serie_boleta = d.fe_serie_boleta.Trim().ToUpperInvariant();
            fe.fe_serie_factura = d.fe_serie_factura.Trim().ToUpperInvariant();
            fe.fe_serie_nc_boleta = d.fe_serie_nc_boleta.Trim().ToUpperInvariant();
            fe.fe_serie_nc_factura = d.fe_serie_nc_factura.Trim().ToUpperInvariant();
            fe.fe_igv = Math.Round(d.fe_igv, 2);
            fe.fe_por_defecto = d.fe_por_defecto is 1 or 3 ? d.fe_por_defecto : (byte)0;

            var faltan = _fe.Pendientes(fe);
            if (d.fe_activa && faltan.Count > 0)
            {
                fe.fe_activa = false;
                TempData["error"] = "Se guardó, pero para activar la facturación falta: " + string.Join(", ", faltan) + ".";
            }
            else
            {
                fe.fe_activa = d.fe_activa;
                TempData["success"] = fe.fe_activa
                    ? $"Facturación electrónica activa en ambiente de {(fe.fe_ambiente == AmbienteFe.Produccion ? "PRODUCCIÓN" : "pruebas")}."
                    : "Configuración guardada. La facturación electrónica está desactivada.";
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(200_000)]
        public async Task<IActionResult> Certificado(IFormFile archivo, string clave)
        {
            if (archivo == null || archivo.Length == 0 || archivo.Length > 100_000)
            {
                TempData["error"] = "Elija el archivo del certificado (.pfx o .p12, máximo 100 KB).";
                return RedirectToAction(nameof(Index));
            }
            using var ms = new MemoryStream();
            await archivo.CopyToAsync(ms);
            var fe = await ObtenerParaEditarAsync();
            var error = _fe.GuardarCertificado(fe, ms.ToArray(), clave ?? "");
            if (error != null)
            {
                TempData["error"] = error;
                return RedirectToAction(nameof(Index));
            }
            await _context.SaveChangesAsync();
            TempData["success"] = $"Certificado de {fe.fe_certificado_titular} cargado (vence el {fe.fe_certificado_vence:dd/MM/yyyy}).";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CertificadoPrueba()
        {
            var fe = await ObtenerParaEditarAsync();
            if (fe.fe_ambiente != AmbienteFe.Pruebas)
            {
                TempData["error"] = "El certificado de prueba solo sirve en el ambiente de pruebas.";
                return RedirectToAction(nameof(Index));
            }
            if (!Documentos.RucValido(fe.fe_ruc))
            {
                TempData["error"] = "Guarde primero un RUC válido.";
                return RedirectToAction(nameof(Index));
            }
            _fe.GenerarCertificadoPrueba(fe);
            await _context.SaveChangesAsync();
            TempData["success"] = "Se generó un certificado de prueba. Úselo solo con el ambiente de pruebas de SUNAT.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Prueba()
        {
            var r = await _fe.EmitirPruebaAsync();
            if (r.Ok) TempData["success"] = $"SUNAT aceptó la boleta de prueba {r.Comprobante.Numero}. {r.Comprobante.comp_sunat_mensaje}";
            else TempData["error"] = r.Error;
            return RedirectToAction(nameof(Index));
        }

        private void ValidarSerie(string valor, char inicial, string campo, Regex resto)
        {
            valor = valor?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(valor) || valor.Length != 4 || valor[0] != inicial || !resto.IsMatch(valor[1..]))
            {
                ModelState.AddModelError(campo, $"La serie tiene 4 caracteres y empieza con {inicial} (ej.: {inicial}001).");
            }
        }

        private async Task<MDFacturacion> ObtenerParaEditarAsync()
        {
            var fe = await _context.Facturacion.OrderBy(x => x.id_fe).FirstOrDefaultAsync();
            if (fe == null)
            {
                var g = await _context.Generales.OrderBy(x => x.id_gene).FirstOrDefaultAsync();
                fe = new MDFacturacion { fe_ruc = g?.gene_ruc, fe_razon_social = g?.gene_empre, fe_direccion = g?.gene_direc };
                _context.Facturacion.Add(fe);
            }
            return fe;
        }

        private async Task CargarAsync(MDFacturacion fe)
        {
            ViewBag.pendientes = _fe.Pendientes(fe);
            ViewBag.ultimos = await _context.Comprobantes.AsNoTracking().OrderByDescending(c => c.id_comp).Take(5)
                .Select(c => new MDComprobante { id_comp = c.id_comp, comp_tipo = c.comp_tipo, comp_serie = c.comp_serie, comp_numero = c.comp_numero, comp_fecha = c.comp_fecha, comp_total = c.comp_total, comp_estado = c.comp_estado, comp_sunat_mensaje = c.comp_sunat_mensaje, comp_ambiente = c.comp_ambiente })
                .ToListAsync();
        }
    }
}
