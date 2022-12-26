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
    public class MDEgresosController : Controller
    {
        private readonly Datacontext _context;

        public MDEgresosController(Datacontext context)
        {
            _context = context;
        }

        // GET: MDEgresos
        public async Task<IActionResult> Index()
        {
            return View(await _context.Egresos.ToListAsync());
        }

        // GET: MDEgresos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDEgresos = await _context.Egresos
                .FirstOrDefaultAsync(m => m.id_egre == id);
            if (mDEgresos == null)
            {
                return NotFound();
            }

            return View(mDEgresos);
        }

        // GET: MDEgresos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MDEgresos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_egre,egre_descr,egre_costo,egre_fecha")] MDEgresos mDEgresos)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mDEgresos);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(mDEgresos);
        }

        // GET: MDEgresos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDEgresos = await _context.Egresos.FindAsync(id);
            if (mDEgresos == null)
            {
                return NotFound();
            }
            return View(mDEgresos);
        }

        // POST: MDEgresos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_egre,egre_descr,egre_costo,egre_fecha")] MDEgresos mDEgresos)
        {
            if (id != mDEgresos.id_egre)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mDEgresos);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MDEgresosExists(mDEgresos.id_egre))
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
            return View(mDEgresos);
        }

        // GET: MDEgresos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDEgresos = await _context.Egresos
                .FirstOrDefaultAsync(m => m.id_egre == id);
            if (mDEgresos == null)
            {
                return NotFound();
            }

            return View(mDEgresos);
        }

        // POST: MDEgresos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDEgresos = await _context.Egresos.FindAsync(id);
            _context.Egresos.Remove(mDEgresos);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MDEgresosExists(int id)
        {
            return _context.Egresos.Any(e => e.id_egre == id);
        }
    }
}
