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
    public class MDDeliveriesController : Controller
    {
        private readonly Datacontext _context;

        public MDDeliveriesController(Datacontext context)
        {
            _context = context;
        }

        // GET: MDDeliveries
        public async Task<IActionResult> Index()
        {
            return View(await _context.Deliveries.ToListAsync());
        }

        // GET: MDDeliveries/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDDelivery = await _context.Deliveries
                .FirstOrDefaultAsync(m => m.id_deliv == id);
            if (mDDelivery == null)
            {
                return NotFound();
            }

            return View(mDDelivery);
        }

        // GET: MDDeliveries/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MDDeliveries/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_deliv,deliv_codig,deliv_nombcom,deliv_desc,deliv_act")] MDDelivery mDDelivery)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mDDelivery);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(mDDelivery);
        }

        // GET: MDDeliveries/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDDelivery = await _context.Deliveries.FindAsync(id);
            if (mDDelivery == null)
            {
                return NotFound();
            }
            return View(mDDelivery);
        }

        // POST: MDDeliveries/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_deliv,deliv_codig,deliv_nombcom,deliv_desc,deliv_act")] MDDelivery mDDelivery)
        {
            if (id != mDDelivery.id_deliv)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mDDelivery);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MDDeliveryExists(mDDelivery.id_deliv))
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
            return View(mDDelivery);
        }

        // GET: MDDeliveries/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDDelivery = await _context.Deliveries
                .FirstOrDefaultAsync(m => m.id_deliv == id);
            if (mDDelivery == null)
            {
                return NotFound();
            }

            return View(mDDelivery);
        }

        // POST: MDDeliveries/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDDelivery = await _context.Deliveries.FindAsync(id);
            _context.Deliveries.Remove(mDDelivery);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MDDeliveryExists(int id)
        {
            return _context.Deliveries.Any(e => e.id_deliv == id);
        }
    }
}
