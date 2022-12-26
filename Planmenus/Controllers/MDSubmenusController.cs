using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Models;

namespace Planmenus.Controllers
{
    public class MDSubmenusController : Controller
    {
        private readonly Datacontext _context;

        public MDSubmenusController(Datacontext context)
        {
            _context = context;
        }

        // GET: MDSubmenus
        public async Task<IActionResult> Index()
        {
            ViewBag.submenu = await (from submenu in _context.Submenus
                               join menprin in _context.Menuprins on submenu.id_menprin equals menprin.id_menprin
                                select new VMSubmenu
                                {
                                    id_submenu = submenu.id_submenu,
                                    id_menprin = submenu.id_menprin,
                                    menprin_desc = menprin.menuprin_desc,
                                    submenu_desc = submenu.submenu_desc,
                                    submenu_nro = submenu.submenu_nro
                                }).ToListAsync();
            return View();
        }

        // GET: MDSubmenus/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDSubmenu = await _context.Submenus
                .FirstOrDefaultAsync(m => m.id_submenu == id);
            if (mDSubmenu == null)
            {
                return NotFound();
            }

            return View(mDSubmenu);
        }

        // GET: MDSubmenus/Create
        public IActionResult Create()
        {
            ViewBag.menuprin = _context.Menuprins.ToList();
            return View();
        }

        // POST: MDSubmenus/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_submenu,id_menprin,submenu_desc,submenu_nro")] MDSubmenu mDSubmenu)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mDSubmenu);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(mDSubmenu);
        }

        // GET: MDSubmenus/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            ViewBag.menuprin = _context.Menuprins.ToList();
            var mDSubmenu = await _context.Submenus.FindAsync(id);
            if (mDSubmenu == null)
            {
                return NotFound();
            }
            return View(mDSubmenu);
        }

        // POST: MDSubmenus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_submenu,id_menprin,submenu_desc,submenu_nro")] MDSubmenu mDSubmenu)
        {
            if (id != mDSubmenu.id_submenu)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mDSubmenu);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MDSubmenuExists(mDSubmenu.id_submenu))
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
            return View(mDSubmenu);
        }

        // GET: MDSubmenus/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDSubmenu = await _context.Submenus
                .FirstOrDefaultAsync(m => m.id_submenu == id);
            if (mDSubmenu == null)
            {
                return NotFound();
            }

            return View(mDSubmenu);
        }

        // POST: MDSubmenus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDSubmenu = await _context.Submenus.FindAsync(id);
            _context.Submenus.Remove(mDSubmenu);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MDSubmenuExists(int id)
        {
            return _context.Submenus.Any(e => e.id_submenu == id);
        }
    }
}
