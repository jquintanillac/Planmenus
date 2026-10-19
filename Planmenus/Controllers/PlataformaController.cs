using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Planmenus.Data;
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
        public static readonly string[] Planes = { "basico", "estandar", "premium" };

        private readonly Datacontext _context;
        private readonly EmpresaService _empresas;
        private readonly IMemoryCache _cache;

        public PlataformaController(Datacontext context, EmpresaService empresas, IMemoryCache cache)
        {
            _context = context;
            _empresas = empresas;
            _cache = cache;
        }

        public record Fila(int Id, string Nombre, string Codigo, string Ruc, bool Activa, string Plan, DateTime Creada,
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
                    return new Fila(e.id_empresa, e.emp_nombre, e.emp_codigo, e.emp_ruc, e.emp_activa, e.emp_plan, e.emp_creada,
                        u?.Cantidad ?? 0, v?.Monto ?? 0, v?.Tickets ?? 0, u?.Ultimo);
                }).ToList();
            ViewBag.propia = EmpresaPropia();
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
            _cache.Remove("empresa-activa-" + id);
            TempData["success"] = activa ? "Restaurante reactivado." : "Restaurante suspendido: sus usuarios ya no pueden ingresar.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Plan(int id, string plan)
        {
            if (!Planes.Contains(plan)) return BadRequest();
            var n = await _context.Empresas.Where(e => e.id_empresa == id)
                .ExecuteUpdateAsync(s => s.SetProperty(e => e.emp_plan, plan));
            if (n == 0) return NotFound();
            TempData["success"] = $"Plan cambiado a {plan}.";
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
