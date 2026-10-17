#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Models;
using Planmenus.Data.DataSql;
using Planmenus.Helpers;

namespace Planmenus.Controllers
{
    public class MDCajasController : Controller
    {
        private readonly Datacontext _context;
        private readonly SP_Menus _menus;
        
        public MDCajasController(Datacontext context, SP_Menus menus)
        {
            _context = context;
            _menus = menus;
        }

        // GET: MDCajas
        [Permiso("submenu3_2")]
        public async Task<IActionResult> Index(DateTime Fecini, DateTime Fecfin)
        {
            DateTime fecha1 = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "SA Pacific Standard Time");
            DateTime fecha2 = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "SA Pacific Standard Time");

            if (Fecini == DateTime.MinValue && Fecfin == DateTime.MinValue)
            {
                Fecini = fecha1;
                Fecfin = fecha2;
            }
            return View();
        }

        // GET: MDCajas
        [Permiso("submenu3_2")]
        public async Task<IActionResult> Cuadre()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("submenu3_3")]
        public async Task<IActionResult> Anular(int id, string caja_obs)
        {
            var mDCaja = await _context.Cajas.FindAsync(id);
            if (mDCaja == null)
            {
                return NotFound();
            }

            mDCaja.caja_act = false;
            mDCaja.caja_obs = caja_obs;
            await _context.SaveChangesAsync();
            return RedirectToAction("Cajadiaria", "MDCajas");
        }

        // GET: MDCajas
        [Permiso("submenu3_3")]
        public async Task<IActionResult> Cajadiaria(DateTime Fecini, DateTime Fecfin)
        {

            DateTime fecha1 = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "SA Pacific Standard Time");
            DateTime fecha2 = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "SA Pacific Standard Time");

            if (Fecini == DateTime.MinValue && Fecfin == DateTime.MinValue)
            {
                Fecini = fecha1;
                Fecfin = fecha2;
            }
            ViewBag.cajas = await _menus.mSPcajas(Fecini, Fecfin);
            return View();
        }

        [Permiso("submenu3_3")]
        public async Task<IActionResult> Imprimir(int? id)
        {
            return RedirectToAction("Index", "Crearpdf", new { id_caja = id });
        }

        [Permiso("submenu3_4")]
        public async Task<IActionResult> RptListpagos(DateTime Fecini, DateTime Fecfin)
        {
            DateTime fecha1 = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "SA Pacific Standard Time");
            DateTime fecha2 = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "SA Pacific Standard Time");

            if(Fecini == DateTime.MinValue && Fecfin == DateTime.MinValue)
            {
                Fecini = fecha1;
                Fecfin = fecha2;
            }

            ViewBag.reporteQ01 = await _menus.mReporteQ01(Fecini, Fecfin);
            return View();
        }

        [Permiso("submenu3_5")]
        public async Task<IActionResult> Rpttipodepago(DateTime Fecini, DateTime Fecfin)
        {
            DateTime fecha1 = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "SA Pacific Standard Time");
            DateTime fecha2 = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "SA Pacific Standard Time");
            if (Fecini == DateTime.MinValue && Fecfin == DateTime.MinValue)
            {
                Fecini = fecha1;
                Fecfin = fecha2;
            }
            ViewBag.reporteQ02 = await _menus.mReporteQ02(Fecini, Fecfin);
            return View();
        }

        [Permiso("submenu3_6")]
        public async Task<IActionResult> Rptporplato(DateTime Fecini, DateTime Fecfin)
        {
            DateTime fecha1 = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "SA Pacific Standard Time");
            DateTime fecha2 = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "SA Pacific Standard Time");
            if (Fecini == DateTime.MinValue && Fecfin == DateTime.MinValue)
            {
                Fecini = fecha1;
                Fecfin = fecha2;
            }
            ViewBag.reporteQ03 = await _menus.mReporteQ03(Fecini, Fecfin);
            return View();
        }
        [Permiso("submenu3_7")]
        public async Task<IActionResult> Rptcantplato(DateTime Fecini, DateTime Fecfin)
        {
            DateTime fecha1 = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "SA Pacific Standard Time");
            DateTime fecha2 = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "SA Pacific Standard Time");
            if (Fecini == DateTime.MinValue && Fecfin == DateTime.MinValue)
            {
                Fecini = fecha1;
                Fecfin = fecha2;
            }
            ViewBag.reporteQ04 = await _menus.mReporteQ04(Fecini, Fecfin);
            return View();
        }

        // GET: MDCajas/Details/5
        [Permiso("submenu3_3")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDCaja = await _context.Cajas
                .FirstOrDefaultAsync(m => m.id_caja == id);
            if (mDCaja == null)
            {
                return NotFound();
            }

            return View(mDCaja);
        }

        // GET: MDCajas/Create
        [Permiso("submenu3_1")]
        public async Task<IActionResult> Create()
        {
            DateTime fecha = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "SA Pacific Standard Time");
            ViewBag.delivery = await _context.Deliveries.ToListAsync();
            ViewBag.menu = await _context.Menus.Where(x => x.menu_fecha.Date == fecha.Date).ToListAsync();
            ViewBag.tipago = await _context.Tipo_Pagos.ToListAsync();
            ViewBag.mesa = await _context.Mesas.ToListAsync();            
            return View();
        }

        // POST: MDCajas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Permiso("submenu3_1")]
        public async Task<IActionResult> Create([Bind("id_menu,caja_monto,id_tipago,id_mesa,id_deliv,caja_obs")] MDCaja mDCaja)
        {
            DateTime fecha = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.Now, "SA Pacific Standard Time");
            try
            {
                MDCaja oCaja = new MDCaja
                {
                    id_menu = mDCaja.id_menu,
                    id_mesa = mDCaja.id_mesa,
                    id_tipago = mDCaja.id_tipago,
                    id_deliv = mDCaja.id_deliv,
                    caja_obs = mDCaja.caja_obs,
                    caja_monto = mDCaja.caja_monto,
                    caja_fecha = fecha,
                    caja_act = true
                };

                _context.Add(oCaja);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Crearpdf", new { id_caja = oCaja.id_caja });
            }
            catch (Exception)
            {
                throw;              
            }

           // return RedirectToAction("Create", "MDCajas")
        }

        // GET: MDCajas/Edit/5
        [Permiso("submenu3_3")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDCaja = await _context.Cajas.FindAsync(id);
            if (mDCaja == null)
            {
                return NotFound();
            }
            return View(mDCaja);
        }

        // GET: MDCajas/Delete/5
        [Permiso("submenu3_3")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mDCaja = await _context.Cajas
                .FirstOrDefaultAsync(m => m.id_caja == id);
            if (mDCaja == null)
            {
                return NotFound();
            }

            return View(mDCaja);
        }

        // POST: MDCajas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Permiso("submenu3_3")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mDCaja = await _context.Cajas.FindAsync(id);
            _context.Cajas.Remove(mDCaja);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
