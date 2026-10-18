using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Helpers;
using Planmenus.Data.Entities;
using Planmenus.Models;

namespace Planmenus.Controllers
{
    [Permiso("submenu1_7")]
    public class MDRol_PermisosController : Controller
    {
        private readonly Datacontext _context;

        public MDRol_PermisosController(Datacontext context)
        {
            _context = context;
        }

        // GET: MDRol_Permisos
        public async Task<IActionResult> Index(int id_rol)
        {
            var Idrol = 0;
            if (id_rol == 0)
            {
                Idrol = 1;
            }
            else
            {
                Idrol = id_rol;
            }
            ViewData["Idrol2"] = id_rol;

            // Crea, desactivadas, las filas de permisos que falten (roles nuevos o submenus agregados despues)
            if (await _context.Rols.AnyAsync(r => r.id_rol == Idrol))
            {
                var existentes = await _context.Rol_Permisos.Where(x => x.id_rol == Idrol).Select(x => x.id_submenu).ToListAsync();
                var faltantes = await _context.Submenus.Where(s => !existentes.Contains(s.id_submenu)).ToListAsync();
                if (faltantes.Count > 0)
                {
                    _context.Rol_Permisos.AddRange(faltantes.Select(s => new MDRol_Permisos
                    {
                        id_rol = Idrol,
                        id_menprin = s.id_menprin,
                        id_submenu = s.id_submenu,
                        rolper_act = false
                    }));
                    await _context.SaveChangesAsync();
                }
            }
            ViewBag.rol = await _context.Rols.ToListAsync();  
            ViewBag.admin = await (from rolper in _context.Rol_Permisos
                             join rol in _context.Rols on rolper.id_rol equals rol.id_rol
                             join menu in _context.Menuprins on rolper.id_menprin equals menu.id_menprin
                             join subm in _context.Submenus on rolper.id_submenu equals subm.id_submenu
                             where rolper.id_rol == Idrol && rolper.id_menprin == 1
                             select new VMRolpers
                             {
                                 id_rolper = rolper.id_rolper,
                                 //id_rol = rolper.id_rol,
                                 //id_menprin = rolper.id_menprin,
                                 //id_submenu = rolper.id_submenu,
                                 rol_desc = rol.rol_desc,
                                 menuprin_desc = menu.menuprin_desc,
                                 submenu_desc = subm.submenu_desc,
                                 rolper_act = rolper.rolper_act
                             }).ToListAsync();
            ViewBag.plani = await (from rolper in _context.Rol_Permisos
                             join rol in _context.Rols on rolper.id_rol equals rol.id_rol
                             join menu in _context.Menuprins on rolper.id_menprin equals menu.id_menprin
                             join subm in _context.Submenus on rolper.id_submenu equals subm.id_submenu
                             where rolper.id_rol == Idrol && rolper.id_menprin == 2
                             select new VMRolpers
                             {
                                 id_rolper = rolper.id_rolper,
                                 //id_rol = rolper.id_rol,
                                 //id_menprin = rolper.id_menprin,
                                 //id_submenu = rolper.id_submenu,
                                 rol_desc = rol.rol_desc,
                                 menuprin_desc = menu.menuprin_desc,
                                 submenu_desc = subm.submenu_desc,
                                 rolper_act = rolper.rolper_act
                             }).ToListAsync();
            ViewBag.caja  = await (from rolper in _context.Rol_Permisos
                             join rol in _context.Rols on rolper.id_rol equals rol.id_rol
                             join menu in _context.Menuprins on rolper.id_menprin equals menu.id_menprin
                             join subm in _context.Submenus on rolper.id_submenu equals subm.id_submenu
                             where rolper.id_rol == Idrol && rolper.id_menprin == 3
                             select new VMRolpers
                             {
                                 id_rolper = rolper.id_rolper,
                                 //id_rol = rolper.id_rol,
                                 //id_menprin = rolper.id_menprin,
                                 //id_submenu = rolper.id_submenu,
                                 rol_desc = rol.rol_desc,
                                 menuprin_desc = menu.menuprin_desc,
                                 submenu_desc = subm.submenu_desc,
                                 rolper_act = rolper.rolper_act
                             }).ToListAsync();
            return View();
        }

        // GET: MDRol_Permisos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDRol_Permisos = await _context.Rol_Permisos
                .FirstOrDefaultAsync(m => m.id_rolper == id);
            if (mDRol_Permisos == null)
            {
                return NotFound();
            }

            return View(mDRol_Permisos);
        }

        // GET: MDRol_Permisos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MDRol_Permisos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_rolper,id_menprin,id_submenu,rolper_act")] MDRol_Permisos mDRol_Permisos)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mDRol_Permisos);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(mDRol_Permisos);
        }

        // GET: MDRol_Permisos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDRol_Permisos = await _context.Rol_Permisos.FindAsync(id);
            if (mDRol_Permisos == null)
            {
                return NotFound();
            }
            return View(mDRol_Permisos);
        }

        // POST: MDRol_Permisos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_rolper,id_menprin,id_submenu,rolper_act")] MDRol_Permisos mDRol_Permisos)
        {
            if (id != mDRol_Permisos.id_rolper)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mDRol_Permisos);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MDRol_PermisosExists(mDRol_Permisos.id_rolper))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(mDRol_Permisos);
        }

        // GET: MDRol_Permisos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDRol_Permisos = await _context.Rol_Permisos
                .FirstOrDefaultAsync(m => m.id_rolper == id);
            if (mDRol_Permisos == null)
            {
                return NotFound();
            }

            return View(mDRol_Permisos);
        }

        // POST: MDRol_Permisos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDRol_Permisos = await _context.Rol_Permisos.FindAsync(id);
            _context.Rol_Permisos.Remove(mDRol_Permisos);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MDRol_PermisosExists(int id)
        {
            return _context.Rol_Permisos.Any(e => e.id_rolper == id);
        }

        /// <summary>Deja activos solo los permisos marcados del rol (los demas se desactivan)</summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> otorgar(List<int> menus, int Idrol)
        {
            var permisos = await _context.Rol_Permisos.Where(x => x.id_rol == Idrol).ToListAsync();
            if (permisos.Count == 0)
            {
                return NotFound(new { ok = false, mensaje = "El rol no existe o no tiene permisos." });
            }
            var marcados = (menus ?? new List<int>()).ToHashSet();
            foreach (var p in permisos)
            {
                // Solo se modifican filas de este rol, aunque lleguen ids de otros roles
                p.rolper_act = marcados.Contains(p.id_rolper);
            }
            await _context.SaveChangesAsync();
            return Json(new { ok = true, activos = permisos.Count(p => p.rolper_act) });
        }


    }
}
