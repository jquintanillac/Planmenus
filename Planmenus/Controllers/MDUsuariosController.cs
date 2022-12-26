#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;

namespace Planmenus.Controllers
{
    public class MDUsuariosController : Controller
    {
        private readonly Datacontext _context;

        public MDUsuariosController(Datacontext context)
        {
            _context = context;
        }

        // GET: MDUsuarios
        public async Task<IActionResult> Index()
        {
            return View(await _context.Usuarios.ToListAsync());
        }

        // GET: MDUsuarios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDUsuarios = await _context.Usuarios
                .FirstOrDefaultAsync(m => m.IdUsuario == id);
            if (mDUsuarios == null)
            {
                return NotFound();
            }

            return View(mDUsuarios);
        }

        // GET: MDUsuarios/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MDUsuarios/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("IdUsuario,Usua_user,Usua_nombres,Usua_apellidos,Usua_email,Usua_pass,Usua_Hash,Usua_act")] MDUsuarios mDUsuarios)
        {
            if (ModelState.IsValid)
            {
                var hash = HashHelper.Hash(mDUsuarios.Usua_pass);
                MDUsuarios oUsuarios = new MDUsuarios
                {
                    IdUsuario = mDUsuarios.IdUsuario,
                    Usua_user = mDUsuarios.Usua_user,
                    Usua_nombres = mDUsuarios.Usua_nombres,
                    Usua_apellidos = mDUsuarios.Usua_apellidos,
                    Usua_email = mDUsuarios.Usua_email,
                    Usua_pass = mDUsuarios.Usua_pass,
                    Usua_Hash = hash.Salt,
                    Usua_act = mDUsuarios.Usua_act
                };

                _context.Add(oUsuarios);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(mDUsuarios);
        }

        // GET: MDUsuarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDUsuarios = await _context.Usuarios.FindAsync(id);
            if (mDUsuarios == null)
            {
                return NotFound();
            }
            return View(mDUsuarios);
        }

        // POST: MDUsuarios/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdUsuario,Usua_user,Usua_nombres,Usua_apellidos,Usua_email,Usua_pass,Usua_Hash,Usua_act")] MDUsuarios mDUsuarios)
        {
            if (id != mDUsuarios.IdUsuario)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mDUsuarios);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MDUsuariosExists(mDUsuarios.IdUsuario))
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
            return View(mDUsuarios);
        }

        // GET: MDUsuarios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDUsuarios = await _context.Usuarios
                .FirstOrDefaultAsync(m => m.IdUsuario == id);
            if (mDUsuarios == null)
            {
                return NotFound();
            }

            return View(mDUsuarios);
        }

        // POST: MDUsuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDUsuarios = await _context.Usuarios.FindAsync(id);
            _context.Usuarios.Remove(mDUsuarios);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MDUsuariosExists(int id)
        {
            return _context.Usuarios.Any(e => e.IdUsuario == id);
        }
    }
}
