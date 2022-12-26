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
    public class MDInicialsController : Controller
    {
        private readonly Datacontext _context;

        public MDInicialsController(Datacontext context)
        {
            _context = context;
        }

        // GET: MDInicials
        public async Task<IActionResult> Index()
        {
            return View(await _context.Inicial.ToListAsync());
        }

        // GET: MDInicials/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDInicial = await _context.Inicial
                .FirstOrDefaultAsync(m => m.id_ini == id);
            if (mDInicial == null)
            {
                return NotFound();
            }

            return View(mDInicial);
        }

        // GET: MDInicials/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MDInicials/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_ini,ini_desc,ini_monto,ini_fecha")] MDInicial mDInicial)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mDInicial);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(mDInicial);
        }

        // GET: MDInicials/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDInicial = await _context.Inicial.FindAsync(id);
            if (mDInicial == null)
            {
                return NotFound();
            }
            return View(mDInicial);
        }

        // POST: MDInicials/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_ini,ini_desc,ini_monto,ini_fecha")] MDInicial mDInicial)
        {
            if (id != mDInicial.id_ini)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mDInicial);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MDInicialExists(mDInicial.id_ini))
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
            return View(mDInicial);
        }

        // GET: MDInicials/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDInicial = await _context.Inicial
                .FirstOrDefaultAsync(m => m.id_ini == id);
            if (mDInicial == null)
            {
                return NotFound();
            }

            return View(mDInicial);
        }

        // POST: MDInicials/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDInicial = await _context.Inicial.FindAsync(id);
            _context.Inicial.Remove(mDInicial);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MDInicialExists(int id)
        {
            return _context.Inicial.Any(e => e.id_ini == id);
        }
    }
}
