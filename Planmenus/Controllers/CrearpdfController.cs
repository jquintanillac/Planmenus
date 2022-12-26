using Microsoft.AspNetCore.Mvc;
using Planmenus.Data;
using Planmenus.Data.DataSql;
using Planmenus.Data.Entities;
using Planmenus.Models;
using Rotativa.AspNetCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planmenus.Controllers
{
    public class CrearpdfController : Controller
    {
        private readonly Datacontext _context;
        private readonly SP_Menus _menus;

        public CrearpdfController (Datacontext context)
        {
            _context = context;
            _menus = new SP_Menus();
        }
        public IActionResult Index(int id_caja)
        {
            var vcaja =  (from caj in _context.Cajas
                         join men in _context.Menus on caj.id_menu equals men.id_menu
                         where caj.id_caja == id_caja
                         select new VMCaja
                         {
                            ticket = caj.id_caja,
                            menu = men.menu_desc,
                            monto = caj.caja_monto,
                            fecha = caj.caja_fecha
                         }).FirstOrDefault();
            return new ViewAsPdf("Index", vcaja)
            {
                PageSize= Rotativa.AspNetCore.Options.Size.A8
            };
        }

        public async Task<IActionResult> RptCuadre(DateTime Fecini, DateTime Fecfin)
        {
            List<VMCaja_Q05> listQ05 = new List<VMCaja_Q05>();
            listQ05 = await _menus.mReporteQ05(Fecini, Fecfin);
            return new ViewAsPdf("RptCuadre", listQ05)
            {
                PageSize = Rotativa.AspNetCore.Options.Size.A4
            };
        }

    }
}
