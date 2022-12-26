using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;

namespace Planmenus.Controllers
{
    public class MDRolsController : Controller
    {
        private readonly Datacontext _context;

        public MDRolsController(Datacontext context)
        {
            _context = context;
        }

        // GET: MDRols
        public async Task<IActionResult> Index()
        {
            return View(await _context.Rols.ToListAsync());
        }

        // GET: MDRols/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDRol = await _context.Rols
                .FirstOrDefaultAsync(m => m.id_rol == id);
            if (mDRol == null)
            {
                return NotFound();
            }

            return View(mDRol);
        }

        // GET: MDRols/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MDRols/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_rol,rol_desc,rol_act")] MDRol mDRol)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mDRol);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(mDRol);
        }

        // GET: MDRols/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDRol = await _context.Rols.FindAsync(id);
            if (mDRol == null)
            {
                return NotFound();
            }
            return View(mDRol);
        }

        // POST: MDRols/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_rol,rol_desc,rol_act")] MDRol mDRol)
        {
            if (id != mDRol.id_rol)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mDRol);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MDRolExists(mDRol.id_rol))
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
            return View(mDRol);
        }

        // GET: MDRols/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDRol = await _context.Rols
                .FirstOrDefaultAsync(m => m.id_rol == id);
            if (mDRol == null)
            {
                return NotFound();
            }

            return View(mDRol);
        }

        // POST: MDRols/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDRol = await _context.Rols.FindAsync(id);
            _context.Rols.Remove(mDRol);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MDRolExists(int id)
        {
            return _context.Rols.Any(e => e.id_rol == id);
        }
    }
}
