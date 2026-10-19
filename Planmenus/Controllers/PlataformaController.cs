using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Services;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Panel del dueño de la plataforma: ve y administra todos los restaurantes (empresas).
    /// Lee entre empresas con IgnoreQueryFilters, por eso solo entra el superadministrador.
    /// </summary>
    [Authorize(Policy = PoliticaSuperadmin)]
    public class PlataformaController : Controller
    {
        public const string PoliticaSuperadmin = "superadmin";
        public static readonly string[] MediosPago = { "Yape", "Plin", "Transferencia", "Tarjeta", "Efectivo", "Otro" };

        private readonly Datacontext _context;
        private readonly EmpresaService _empresas;
        private readonly IMemoryCache _cache;
        private readonly SuscripcionOptions _suscripcion;

        public PlataformaController(Datacontext context, EmpresaService empresas, IMemoryCache cache, IOptions<SuscripcionOptions> suscripcion)
        {
            _context = context;
            _empresas = empresas;
            _cache = cache;
            _suscripcion = suscripcion.Value;
        }

        public record Fila(int Id, string Nombre, string Codigo, string Ruc, bool Activa, string Plan, DateTime Creada, DateTime? Vence,
                           int Usuarios, decimal Ventas30, int Tickets30, DateTime? UltimoIngreso);

        public async Task<IActionResult> Index()
        {
            var desde = Fechas.HoyPeru().AddDays(-30);
            var usuarios = await _context.Usuarios.IgnoreQueryFilters().AsNoTracking()
                .GroupBy(u => u.id_empresa)
                .Select(g => new { g.Key, Cantidad = g.Count(u => u.Usua_act), Ultimo = g.Max(u => u.Usua_ultimo_ingreso) })
                .ToDictionaryAsync(x => x.Key);
            var ventas = await _context.Cajas.IgnoreQueryFilters().AsNoTracking()
                .Where(c => c.caja_act && c.caja_pagado && c.caja_fecha >= desde)
                .GroupBy(c => c.id_empresa)
                .Select(g => new { g.Key, Monto = g.Sum(c => c.caja_monto), Tickets = g.Count() })
                .ToDictionaryAsync(x => x.Key);

            var filas = (await _context.Empresas.AsNoTracking().OrderBy(e => e.id_empresa).ToListAsync())
                .Select(e =>
                {
                    usuarios.TryGetValue(e.id_empresa, out var u);
                    ventas.TryGetValue(e.id_empresa, out var v);
                    return new Fila(e.id_empresa, e.emp_nombre, e.emp_codigo, e.emp_ruc, e.emp_activa, e.emp_plan, e.emp_creada, e.emp_vence,
                        u?.Cantidad ?? 0, v?.Monto ?? 0, v?.Tickets ?? 0, u?.Ultimo);
                }).ToList();
            ViewBag.propia = EmpresaPropia();
            ViewBag.planes = _suscripcion.Todos;
            var inicioMes = new DateTime(Fechas.HoyPeru().Year, Fechas.HoyPeru().Month, 1);
            ViewBag.cobrado_mes = await _context.Pagos_Suscripcion.IgnoreQueryFilters()
                .Where(p => p.pago_fecha >= inicioMes).SumAsync(p => (decimal?)p.pago_monto) ?? 0m;
            var nombres = filas.ToDictionary(f => f.Id, f => f.Nombre);
            ViewBag.pagos = (await _context.Pagos_Suscripcion.IgnoreQueryFilters().AsNoTracking()
                    .OrderByDescending(p => p.id_pago).Take(20).ToListAsync())
                .Select(p => (Pago: p, Empresa: nombres.GetValueOrDefault(p.id_empresa))).ToList();
            return View(filas);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Estado(int id, bool activa)
        {
            if (!activa && id == EmpresaPropia())
            {
                TempData["error"] = "No puede suspender su propia empresa.";
                return RedirectToAction(nameof(Index));
            }
            var n = await _context.Empresas.Where(e => e.id_empresa == id)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.emp_activa, activa));
            if (n == 0) return NotFound();
            _cache.Remove(EstadoEmpresa.Clave(id));
            TempData["success"] = activa ? "Restaurante reactivado." : "Restaurante suspendido: sus usuarios ya no pueden ingresar.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Plan(int id, string plan)
        {
            if (!_suscripcion.Todos.Any(p => p.Codigo == plan)) return BadRequest();
            var n = await _context.Empresas.Where(e => e.id_empresa == id)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.emp_plan, plan));
            if (n == 0) return NotFound();
            TempData["success"] = $"Plan cambiado a {_suscripcion.Buscar(plan).Nombre}.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Registra el pago de la suscripcion: fija el plan y extiende el vencimiento desde hoy
        /// o desde el vencimiento actual si todavia no llega.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pago(int id, string plan, int meses, decimal? monto, string medio, string referencia)
        {
            var p = _suscripcion.Todos.FirstOrDefault(x => x.Codigo == plan);
            if (p == null || meses is < 1 or > 24 || !MediosPago.Contains(medio)) return BadRequest();
            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.id_empresa == id);
            if (empresa == null) return NotFound();

            var hoy = Fechas.HoyPeru();
            var desde = empresa.emp_vence > hoy ? empresa.emp_vence.Value : hoy;
            empresa.emp_vence = desde.AddMonths(meses);
            empresa.emp_plan = p.Codigo;
            empresa.emp_activa = true;
            await _context.SaveChangesAsync();

            // El pago pertenece a esa empresa: se guarda dentro de ella (asi lo ve en Mi suscripcion)
            var usuario = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            await _empresas.EnEmpresaAsync(id, async sp =>
            {
                var db = sp.GetRequiredService<Datacontext>();
                db.Pagos_Suscripcion.Add(new MDPago_Suscripcion
                {
                    pago_fecha = Fechas.AhoraPeru(), pago_monto = Math.Round(monto ?? p.PrecioMes * meses, 2), pago_meses = meses,
                    pago_plan = p.Codigo, pago_medio = medio, pago_referencia = referencia?.Trim(), pago_vence = empresa.emp_vence.Value, pago_usuario = usuario
                });
                return await db.SaveChangesAsync();
            });
            _cache.Remove(EstadoEmpresa.Clave(id));
            TempData["success"] = $"Pago registrado: {empresa.emp_nombre} en plan {p.Nombre} hasta el {empresa.emp_vence:dd/MM/yyyy}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(string nombre, string codigo, string ruc, string usuario, string admin_nombre, string clave, string email)
        {
            var alta = await _empresas.CrearAsync(new EmpresaService.Alta(nombre, codigo, ruc, usuario, admin_nombre, clave, email));
            if (alta.Ok)
            {
                TempData["success"] = $"Restaurante \"{alta.Empresa.emp_nombre}\" creado. Su administrador ingresa con el usuario {usuario.Trim().ToLowerInvariant()}.";
            }
            else
            {
                TempData["error"] = alta.Error;
            }
            return RedirectToAction(nameof(Index));
        }

        private int EmpresaPropia() => int.TryParse(User.FindFirst(SessionHelper.ClaimEmpresa)?.Value, out var id) ? id : 0;
    }
}
