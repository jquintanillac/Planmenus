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
    public class MDIngresosController : Controller
    {
        private readonly Datacontext _context;

        public MDIngresosController(Datacontext context)
        {
            _context = context;
        }

        // GET: MDIngresos
        public async Task<IActionResult> Index()
        {
            return View(await _context.Ingresos.ToListAsync());
        }

        // GET: MDIngresos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDIngresos = await _context.Ingresos
                .FirstOrDefaultAsync(m => m.id_ingre == id);
            if (mDIngresos == null)
            {
                return NotFound();
            }

            return View(mDIngresos);
        }

        // GET: MDIngresos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MDIngresos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_ingre,ingre_desc,ingre_monto,ingre_fecha")] MDIngresos mDIngresos)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mDIngresos);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(mDIngresos);
        }

        // GET: MDIngresos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDIngresos = await _context.Ingresos.FindAsync(id);
            if (mDIngresos == null)
            {
                return NotFound();
            }
            return View(mDIngresos);
        }

        // POST: MDIngresos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_ingre,ingre_desc,ingre_monto,ingre_fecha")] MDIngresos mDIngresos)
        {
            if (id != mDIngresos.id_ingre)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mDIngresos);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MDIngresosExists(mDIngresos.id_ingre))
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
            return View(mDIngresos);
        }

        // GET: MDIngresos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDIngresos = await _context.Ingresos
                .FirstOrDefaultAsync(m => m.id_ingre == id);
            if (mDIngresos == null)
            {
                return NotFound();
            }

            return View(mDIngresos);
        }

        // POST: MDIngresos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDIngresos = await _context.Ingresos.FindAsync(id);
            _context.Ingresos.Remove(mDIngresos);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MDIngresosExists(int id)
        {
            return _context.Ingresos.Any(e => e.id_ingre == id);
        }
    }
}
