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
    public class MDMesasController : Controller
    {
        private readonly Datacontext _context;

        public MDMesasController(Datacontext context)
        {
            _context = context;
        }

        // GET: MDMesas
        public async Task<IActionResult> Index()
        {
            return View(await _context.Mesas.ToListAsync());
        }

        // GET: MDMesas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDMesas = await _context.Mesas
                .FirstOrDefaultAsync(m => m.id_mesa == id);
            if (mDMesas == null)
            {
                return NotFound();
            }

            return View(mDMesas);
        }

        // GET: MDMesas/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MDMesas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_mesa,mesa_desc,mesa_nro,mesa_act")] MDMesas mDMesas)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mDMesas);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(mDMesas);
        }

        // GET: MDMesas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDMesas = await _context.Mesas.FindAsync(id);
            if (mDMesas == null)
            {
                return NotFound();
            }
            return View(mDMesas);
        }

        // POST: MDMesas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_mesa,mesa_desc,mesa_nro,mesa_act")] MDMesas mDMesas)
        {
            if (id != mDMesas.id_mesa)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mDMesas);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MDMesasExists(mDMesas.id_mesa))
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
            return View(mDMesas);
        }

        // GET: MDMesas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDMesas = await _context.Mesas
                .FirstOrDefaultAsync(m => m.id_mesa == id);
            if (mDMesas == null)
            {
                return NotFound();
            }

            return View(mDMesas);
        }

        // POST: MDMesas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDMesas = await _context.Mesas.FindAsync(id);
            _context.Mesas.Remove(mDMesas);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MDMesasExists(int id)
        {
            return _context.Mesas.Any(e => e.id_mesa == id);
        }
    }
}
