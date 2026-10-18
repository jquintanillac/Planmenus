using Microsoft.AspNetCore.Mvc;
using Planmenus.Data.DataSql;
using Planmenus.Helpers;
using Planmenus.Services;

namespace Planmenus.Controllers
{
    public class CrearpdfController : Controller
    {
        private readonly SP_Menus _menus;
        private readonly TicketService _tickets;
        private readonly PdfService _pdf;

        public CrearpdfController(SP_Menus menus, TicketService tickets, PdfService pdf)
        {
            _menus = menus;
            _tickets = tickets;
            _pdf = pdf;
        }

        [Permiso("submenu3_1", "submenu3_3")]
        public async Task<IActionResult> Index(int id_caja)
        {
            var ticket = await _tickets.ObtenerAsync(id_caja);
            if (ticket == null)
            {
                return NotFound();
            }
            return File(_pdf.Ticket(ticket), "application/pdf");
        }

        [Permiso("submenu3_2")]
        public async Task<IActionResult> RptCuadre(DateTime Fecini, DateTime Fecfin)
        {
            if (Fecini == DateTime.MinValue && Fecfin == DateTime.MinValue)
            {
                Fecini = Fecfin = Fechas.HoyPeru();
            }
            var dias = await _menus.mReporteQ05(Fecini, Fecfin);
            var empresa = await _tickets.ObtenerEmpresaAsync();
            return File(_pdf.Cuadre(empresa.gene_empre, Fecini, Fecfin, dias), "application/pdf");
        }
    }
}
