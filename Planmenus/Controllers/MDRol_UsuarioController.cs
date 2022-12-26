using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Models;

namespace Planmenus.Controllers
{
    public class MDRol_UsuarioController : Controller
    {
        private readonly Datacontext _context;

        public MDRol_UsuarioController(Datacontext context)
        {
            _context = context;
        }

        // GET: MDRol_Usuario
        public async Task<IActionResult> Index()
        {
            ViewBag.roluser = await (from roluser in _context.Rol_Usuarios
                                     join rol in _context.Rols on roluser.id_rol equals rol.id_rol
                                     join user in _context.Usuarios on roluser.IdUsuario equals user.IdUsuario
                                     select new VMRoluser
                                     {
                                         id_roluser = roluser.id_roluser,
                                         IdUsuario = roluser.IdUsuario,
                                         id_rol = roluser.id_rol,
                                         Usua_user = user.Usua_user,
                                         rol_desc = rol.rol_desc
                                     }).ToListAsync();
            return View();
        }

        // GET: MDRol_Usuario/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDRol_Usuario = await _context.Rol_Usuarios
                .FirstOrDefaultAsync(m => m.id_roluser == id);
            if (mDRol_Usuario == null)
            {
                return NotFound();
            }

            return View(mDRol_Usuario);
        }

        // GET: MDRol_Usuario/Create
        public IActionResult Create()
        {
            ViewBag.usuario = _context.Usuarios.ToList();
            ViewBag.rol = _context.Rols.ToList();
            return View();
        }

        // POST: MDRol_Usuario/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_roluser,IdUsuario,id_rol")] MDRol_Usuario mDRol_Usuario)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mDRol_Usuario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(mDRol_Usuario);
        }

        // GET: MDRol_Usuario/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            ViewBag.usuario = _context.Usuarios.ToList();
            ViewBag.rol = _context.Rols.ToList();
            var mDRol_Usuario = await _context.Rol_Usuarios.FindAsync(id);
            if (mDRol_Usuario == null)
            {
                return NotFound();
            }
            return View(mDRol_Usuario);
        }

        // POST: MDRol_Usuario/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_roluser,IdUsuario,id_rol")] MDRol_Usuario mDRol_Usuario)
        {
            if (id != mDRol_Usuario.id_roluser)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mDRol_Usuario);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MDRol_UsuarioExists(mDRol_Usuario.id_roluser))
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
            return View(mDRol_Usuario);
        }

        // GET: MDRol_Usuario/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDRol_Usuario = await _context.Rol_Usuarios
                .FirstOrDefaultAsync(m => m.id_roluser == id);
            if (mDRol_Usuario == null)
            {
                return NotFound();
            }

            return View(mDRol_Usuario);
        }

        // POST: MDRol_Usuario/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDRol_Usuario = await _context.Rol_Usuarios.FindAsync(id);
            _context.Rol_Usuarios.Remove(mDRol_Usuario);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MDRol_UsuarioExists(int id)
        {
            return _context.Rol_Usuarios.Any(e => e.id_roluser == id);
        }
    }
}
