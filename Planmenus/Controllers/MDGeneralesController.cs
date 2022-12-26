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
    public class MDGeneralesController : Controller
    {
        private readonly Datacontext _context;

        public MDGeneralesController(Datacontext context)
        {
            _context = context;
        }

        // GET: MDGenerales
        public async Task<IActionResult> Index()
        {
            return View(await _context.Generales.ToListAsync());
        }

        // GET: MDGenerales/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDGenerales = await _context.Generales
                .FirstOrDefaultAsync(m => m.id_gene == id);
            if (mDGenerales == null)
            {
                return NotFound();
            }

            return View(mDGenerales);
        }

        // GET: MDGenerales/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MDGenerales/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_gene,gene_empre,gene_ruc,gene_direc,gene_telef,gene_url")] MDGenerales mDGenerales)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mDGenerales);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(mDGenerales);
        }

        // GET: MDGenerales/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDGenerales = await _context.Generales.FindAsync(id);
            if (mDGenerales == null)
            {
                return NotFound();
            }
            return View(mDGenerales);
        }

        // POST: MDGenerales/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_gene,gene_empre,gene_ruc,gene_direc,gene_telef,gene_url")] MDGenerales mDGenerales)
        {
            if (id != mDGenerales.id_gene)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mDGenerales);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MDGeneralesExists(mDGenerales.id_gene))
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
            return View(mDGenerales);
        }

        // GET: MDGenerales/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDGenerales = await _context.Generales
                .FirstOrDefaultAsync(m => m.id_gene == id);
            if (mDGenerales == null)
            {
                return NotFound();
            }

            return View(mDGenerales);
        }

        // POST: MDGenerales/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDGenerales = await _context.Generales.FindAsync(id);
            _context.Generales.Remove(mDGenerales);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MDGeneralesExists(int id)
        {
            return _context.Generales.Any(e => e.id_gene == id);
        }
    }
}
