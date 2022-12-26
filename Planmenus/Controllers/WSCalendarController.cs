using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using System.Linq;
using System.Threading.Tasks;

namespace Planmenus.Controllers
{   
    [Route("api/[controller]")]
    [ApiController]
    public class WSCalendarController : ControllerBase
    {
        private readonly Datacontext _context;

        public WSCalendarController(Datacontext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Fulleventos()
        {
            var events = await _context.Eventos.Select(e => new
            {
                id = e.id,
                title = e.title,
                description = e.Description,
                start = e.StarDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = e.EndDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                Alldays = false
            }).ToListAsync();
            return new JsonResult(events);
        }

    }

}
