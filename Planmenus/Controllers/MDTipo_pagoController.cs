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
    public class MDTipo_pagoController : Controller
    {
        private readonly Datacontext _context;

        public MDTipo_pagoController(Datacontext context)
        {
            _context = context;
        }

        // GET: MDTipo_pago
        public async Task<IActionResult> Index()
        {
            return View(await _context.Tipo_Pagos.ToListAsync());
        }

        // GET: MDTipo_pago/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDTipo_pago = await _context.Tipo_Pagos
                .FirstOrDefaultAsync(m => m.id_tipago == id);
            if (mDTipo_pago == null)
            {
                return NotFound();
            }

            return View(mDTipo_pago);
        }

        // GET: MDTipo_pago/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MDTipo_pago/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_tipago,tipago_desc,tipago_act")] MDTipo_pago mDTipo_pago)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mDTipo_pago);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(mDTipo_pago);
        }

        // GET: MDTipo_pago/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDTipo_pago = await _context.Tipo_Pagos.FindAsync(id);
            if (mDTipo_pago == null)
            {
                return NotFound();
            }
            return View(mDTipo_pago);
        }

        // POST: MDTipo_pago/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_tipago,tipago_desc,tipago_act")] MDTipo_pago mDTipo_pago)
        {
            if (id != mDTipo_pago.id_tipago)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mDTipo_pago);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MDTipo_pagoExists(mDTipo_pago.id_tipago))
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
            return View(mDTipo_pago);
        }

        // GET: MDTipo_pago/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDTipo_pago = await _context.Tipo_Pagos
                .FirstOrDefaultAsync(m => m.id_tipago == id);
            if (mDTipo_pago == null)
            {
                return NotFound();
            }

            return View(mDTipo_pago);
        }

        // POST: MDTipo_pago/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDTipo_pago = await _context.Tipo_Pagos.FindAsync(id);
            _context.Tipo_Pagos.Remove(mDTipo_pago);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MDTipo_pagoExists(int id)
        {
            return _context.Tipo_Pagos.Any(e => e.id_tipago == id);
        }
    }
}
