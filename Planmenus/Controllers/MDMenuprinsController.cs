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
    public class MDMenuprinsController : Controller
    {
        private readonly Datacontext _context;

        public MDMenuprinsController(Datacontext context)
        {
            _context = context;
        }

        // GET: MDMenuprins
        public async Task<IActionResult> Index()
        {
            return View(await _context.Menuprins.ToListAsync());
        }

        // GET: MDMenuprins/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDMenuprin = await _context.Menuprins
                .FirstOrDefaultAsync(m => m.id_menprin == id);
            if (mDMenuprin == null)
            {
                return NotFound();
            }

            return View(mDMenuprin);
        }

        // GET: MDMenuprins/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MDMenuprins/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_menprin,menuprin_desc,menuprin_act")] MDMenuprin mDMenuprin)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mDMenuprin);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(mDMenuprin);
        }

        // GET: MDMenuprins/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDMenuprin = await _context.Menuprins.FindAsync(id);
            if (mDMenuprin == null)
            {
                return NotFound();
            }
            return View(mDMenuprin);
        }

        // POST: MDMenuprins/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_menprin,menuprin_desc,menuprin_act")] MDMenuprin mDMenuprin)
        {
            if (id != mDMenuprin.id_menprin)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mDMenuprin);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MDMenuprinExists(mDMenuprin.id_menprin))
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
            return View(mDMenuprin);
        }

        // GET: MDMenuprins/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDMenuprin = await _context.Menuprins
                .FirstOrDefaultAsync(m => m.id_menprin == id);
            if (mDMenuprin == null)
            {
                return NotFound();
            }

            return View(mDMenuprin);
        }

        // POST: MDMenuprins/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDMenuprin = await _context.Menuprins.FindAsync(id);
            _context.Menuprins.Remove(mDMenuprin);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MDMenuprinExists(int id)
        {
            return _context.Menuprins.Any(e => e.id_menprin == id);
        }
    }
}
