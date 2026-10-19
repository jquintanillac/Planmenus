using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Models;
using Planmenus.Services;
using Planmenus.Services.Facturacion;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Registro publico de restaurantes (cada uno es una empresa nueva) y asistente inicial.
    /// </summary>
    public class RegistroController : Controller
    {
        public const string PoliticaRegistro = "registro";
        private readonly Datacontext _context;
        private readonly EmpresaService _empresas;

        public RegistroController(Datacontext context, EmpresaService empresas)
        {
            _context = context;
            _empresas = empresas;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Inicio", "Home");
            }
            return View(new VMRegistro());
        }

        // GET: Registro/Disponible?codigo=... - para avisar mientras se escribe si la direccion de la carta esta libre
        [AllowAnonymous]
        [EnableRateLimiting(PoliticaRegistro + "-consulta")]
        public async Task<IActionResult> Disponible(string codigo)
        {
            var c = EmpresaService.Codigo(codigo);
            var libre = c.Length >= 3 && !await _context.Empresas.AnyAsync(e => e.emp_codigo == c);
            return Json(new { codigo = c, disponible = libre });
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting(PoliticaRegistro)]
        public async Task<IActionResult> Index(VMRegistro r)
        {
            if (!string.IsNullOrEmpty(r.sitio_web))
            {
                // Robot: se responde como si nada para no darle pistas
                return RedirectToAction(nameof(Index));
            }
            if (!r.acepto)
            {
                ModelState.AddModelError(nameof(r.acepto), "Debe aceptar los términos y la política de privacidad.");
            }
            if (!string.IsNullOrWhiteSpace(r.ruc) && !Documentos.RucValido(r.ruc))
            {
                ModelState.AddModelError(nameof(r.ruc), "El RUC no es válido.");
            }
            if (!ModelState.IsValid)
            {
                return View(r);
            }

            var alta = await _empresas.CrearAsync(new EmpresaService.Alta(r.nombre, r.codigo, r.ruc, r.usuario, r.admin_nombre, r.clave, r.email));
            if (!alta.Ok)
            {
                ModelState.AddModelError("", alta.Error);
                return View(r);
            }

            var usuario = await _context.Usuarios.IgnoreQueryFilters().AsNoTracking()
                .SingleAsync(u => u.Usua_user == r.usuario.Trim().ToLowerInvariant());
            var error = await HttpContext.RequestServices.GetRequiredService<SesionService>().IniciarAsync(HttpContext, usuario);
            if (error != null)
            {
                TempData["error"] = error;
                return RedirectToAction("Index", "Home");
            }
            return RedirectToAction(nameof(Bienvenida));
        }

        // GET: Registro/Bienvenida - asistente inicial (datos, mesas y primeros platos)
        public async Task<IActionResult> Bienvenida()
        {
            ViewBag.empresa = await _context.Generales.AsNoTracking().OrderBy(g => g.id_gene).FirstOrDefaultAsync();
            var modelo = new VMBienvenida();
            for (int i = 0; i < 5; i++) modelo.platos.Add(new VMPlatoInicial());
            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bienvenida(VMBienvenida b)
        {
            var hoy = Fechas.HoyPeru();
            var g = await _context.Generales.OrderBy(x => x.id_gene).FirstOrDefaultAsync();
            if (g != null)
            {
                if (!string.IsNullOrWhiteSpace(b.direccion)) g.gene_direc = b.direccion.Trim();
                if (!string.IsNullOrWhiteSpace(b.telefono)) g.gene_telef = b.telefono.Trim();
            }

            // Mesas en una grilla del plano (se pueden reacomodar despues en Plano del salon)
            var zona = await _context.Zonas.Where(z => z.zona_act).OrderBy(z => z.zona_orden).Select(z => (int?)z.id_zona).FirstOrDefaultAsync();
            var siguiente = (await _context.Mesas.MaxAsync(m => (int?)m.mesa_nro) ?? 0) + 1;
            var disponibles = await HttpContext.RequestServices.GetRequiredService<PlanService>().DisponiblesAsync(Recurso.Mesa);
            int cantidad = Math.Min(Math.Clamp(b.mesas, 0, 60), disponibles), cap = Math.Clamp(b.capacidad, 1, 20);
            for (int i = 0; i < cantidad; i++)
            {
                int nro = siguiente + i, col = i % 6, fila = i / 6;
                _context.Mesas.Add(new MDMesas
                {
                    mesa_nro = nro, mesa_desc = $"Mesa {nro}", mesa_capacidad = cap, mesa_act = true, id_zona = zona,
                    mesa_forma = cap > 4 ? FormaMesa.Rectangular : FormaMesa.Cuadrada,
                    mesa_ancho = cap > 4 ? 150 : 100, mesa_alto = 100,
                    mesa_x = Math.Min(60 + col * 185, 1200 - 150), mesa_y = Math.Min(60 + fila * 170, 800 - 100)
                });
            }

            foreach (var p in (b.platos ?? new()).Where(p => !string.IsNullOrWhiteSpace(p.nombre) && p.precio > 0).Take(10))
            {
                var menu = new MDMenu { menu_desc = p.nombre.Trim(), menu_precio = Math.Round(p.precio.Value, 2), menu_fecha = hoy.AddHours(12) };
                _context.Menus.Add(menu);
                await _context.SaveChangesAsync();
                _context.Eventos.Add(new MDEventos { id_menu = menu.id_menu, title = menu.menu_desc, StarDate = menu.menu_fecha, EndDate = menu.menu_fecha, Description = "Programacion", Location = "" });
            }
            await _context.SaveChangesAsync();
            TempData["success"] = "¡Listo! Su restaurante está configurado. Ya puede tomar su primer pedido.";
            return RedirectToAction("Inicio", "Home");
        }
    }
}
