#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Helpers;
using Planmenus.Data.DataSql;
using Planmenus.Data.Entities;

namespace Planmenus.Controllers
{
    public class MDMenusController : Controller
    {
        private readonly Datacontext _context;
        private readonly SP_Menus _Menu; 

        public MDMenusController(Datacontext context, SP_Menus menus)
        {
            _context = context;
            _Menu = menus;
        }

        // GET: MDMenus
        [Permiso("submenu2_1")]
        public async Task<IActionResult> Index()
        {
            return View(await _context.Menus.ToListAsync());
        }
        [Permiso("submenu2_4")]
        public async Task<IActionResult> Calendar()
        {
            var events = await _context.Eventos.ToListAsync();
            return View();
        }
        [Permiso("submenu2_4")]
        public async Task<IActionResult> Fulleventos()
        {
            var events = await _context.Eventos.Select(e => new
            {
                id = e.id,
                title = e.title,
                description = e.Description,
                start = e.StarDate.ToString("yyyy-MM-dd HH:mm:ss"),
                end = e.EndDate.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToListAsync();
            return new JsonResult(events);
        }


        // GET: MDMenus/Details/5
        [Permiso("submenu2_1")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDMenu = await _context.Menus
                .FirstOrDefaultAsync(m => m.id_menu == id);
            if (mDMenu == null)
            {
                return NotFound();
            }

            return View(mDMenu);
        }

        // GET: MDMenus/Create
        [Permiso("submenu2_1")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: MDMenus/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("submenu2_1")]
        public async Task<IActionResult> Create([Bind("id_menu,menu_desc,menu_precio,menu_observ,menu_fecha")] MDMenu mDMenu)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mDMenu);
                await _context.SaveChangesAsync();

                MDEventos oEventos = new MDEventos
                {
                    id_menu = mDMenu.id_menu,
                    title = mDMenu.menu_desc,
                    StarDate = mDMenu.menu_fecha,
                    EndDate = mDMenu.menu_fecha,
                    Description = "Programacion",
                    Location = ""
                };
                _context.Eventos.Add(oEventos);
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));                               
            }
            return View(mDMenu);
        }

        // GET: MDMenus/Edit/5
        [Permiso("submenu2_1")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDMenu = await _context.Menus.FindAsync(id);
            if (mDMenu == null)
            {
                return NotFound();
            }
            return View(mDMenu);
        }

        // POST: MDMenus/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("submenu2_1")]
        public async Task<IActionResult> Edit(int id, [Bind("id_menu,menu_desc,menu_precio,menu_observ,menu_fecha")] MDMenu mDMenu)
        {
            if (id != mDMenu.id_menu)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mDMenu);
                    await _context.SaveChangesAsync();
                    _Menu.mUpdateEvento(mDMenu.id_menu);

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MDMenuExists(mDMenu.id_menu))
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
            return View(mDMenu);
        }

        // GET: MDMenus/Delete/5
        [Permiso("submenu2_1")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDMenu = await _context.Menus
                .FirstOrDefaultAsync(m => m.id_menu == id);
            if (mDMenu == null)
            {
                return NotFound();
            }

            return View(mDMenu);
        }

        // POST: MDMenus/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Permiso("submenu2_1")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDMenu = await _context.Menus.FindAsync(id);
            if (mDMenu == null)
            {
                return NotFound();
            }
            if (await _context.Caja_Detalles.AnyAsync(d => d.id_menu == id) || await _context.Cajas.AnyAsync(c => c.id_menu == id))
            {
                TempData["error"] = "No se puede eliminar un plato que ya tiene ventas registradas.";
                return RedirectToAction(nameof(Index));
            }
            _context.Menus.Remove(mDMenu);
            await _context.SaveChangesAsync();
            _Menu.mEliminarEvento(id);
            return RedirectToAction(nameof(Index));
        }

        private bool MDMenuExists(int id)
        {
            return _context.Menus.Any(e => e.id_menu == id);
        }
    }
}
