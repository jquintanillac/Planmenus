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
    [Permiso("submenu1_2")]
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
        public async Task<IActionResult> Create([Bind("Usua_user,Usua_nombres,Usua_apellidos,Usua_email,Usua_pass,Usua_act")] MDUsuarios mDUsuarios)
        {
            if (string.IsNullOrWhiteSpace(mDUsuarios.Usua_pass))
            {
                ModelState.AddModelError(nameof(MDUsuarios.Usua_pass), "Ingrese una contraseña.");
            }
            if (await _context.Usuarios.AnyAsync(u => u.Usua_user == mDUsuarios.Usua_user))
            {
                ModelState.AddModelError(nameof(MDUsuarios.Usua_user), "El usuario ya existe.");
            }

            if (ModelState.IsValid)
            {
                var hash = HashHelper.Hash(mDUsuarios.Usua_pass);
                MDUsuarios oUsuarios = new MDUsuarios
                {
                    Usua_user = mDUsuarios.Usua_user,
                    Usua_nombres = mDUsuarios.Usua_nombres,
                    Usua_apellidos = mDUsuarios.Usua_apellidos,
                    Usua_email = mDUsuarios.Usua_email,
                    Usua_pass = hash.Password,
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
            // Nunca se envia el hash al formulario; en blanco = mantener la contraseña actual
            mDUsuarios.Usua_pass = "";
            return View(mDUsuarios);
        }

        // POST: MDUsuarios/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdUsuario,Usua_user,Usua_nombres,Usua_apellidos,Usua_email,Usua_pass,Usua_act")] MDUsuarios mDUsuarios)
        {
            if (id != mDUsuarios.IdUsuario)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    return NotFound();
                }

                usuario.Usua_user = mDUsuarios.Usua_user;
                usuario.Usua_nombres = mDUsuarios.Usua_nombres;
                usuario.Usua_apellidos = mDUsuarios.Usua_apellidos;
                usuario.Usua_email = mDUsuarios.Usua_email;
                usuario.Usua_act = mDUsuarios.Usua_act;
                if (!string.IsNullOrWhiteSpace(mDUsuarios.Usua_pass))
                {
                    var hash = HashHelper.Hash(mDUsuarios.Usua_pass);
                    usuario.Usua_pass = hash.Password;
                    usuario.Usua_Hash = hash.Salt;
                }

                try
                {
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
