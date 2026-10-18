using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Services;

namespace Planmenus.Controllers
{
    /// <summary>
    /// Reservas de mesas y eventos con calendario y avisos por correo.
    /// </summary>
    [Permiso("submenu3_11")]
    public class ReservasController : Controller
    {
        private readonly Datacontext _context;
        private readonly ReservaService _reservas;
        private readonly TicketService _tickets;

        public ReservasController(Datacontext context, ReservaService reservas, TicketService tickets)
        {
            _context = context;
            _reservas = reservas;
            _tickets = tickets;
        }

        public async Task<IActionResult> Index()
        {
            var ahora = Fechas.AhoraPeru();
            var manana = ahora.Date.AddDays(2);
            ViewBag.proximas = await _context.Reservas.AsNoTracking()
                .Where(r => r.res_fin > ahora && r.res_inicio < manana && (r.res_estado == EstadoReserva.Pendiente || r.res_estado == EstadoReserva.Confirmada))
                .OrderBy(r => r.res_inicio).Take(20).ToListAsync();
            return View();
        }

        // GET: Reservas/Eventos?start=...&end=... - origen de datos del calendario
        public async Task<IActionResult> Eventos(string start, string end)
        {
            // El calendario envia la hora local con zona (2026-10-12T00:00:00-05:00): se usa la hora local tal cual,
            // sin convertir, porque las reservas se guardan en hora de Peru
            if (!FechaLocal(start, out var desde) || !FechaLocal(end, out var hasta))
            {
                return BadRequest();
            }
            return await EventosAsync(desde, hasta);
        }

        private static bool FechaLocal(string valor, out DateTime fecha) =>
            DateTime.TryParseExact(valor?.Length >= 19 ? valor[..19] : valor, new[] { "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd" },
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out fecha);

        private async Task<IActionResult> EventosAsync(DateTime start, DateTime end)
        {
            var reservas = await _context.Reservas.AsNoTracking()
                .Where(r => r.res_inicio < end && r.res_fin > start)
                .ToListAsync();
            var ids = reservas.Select(r => r.id_reserva).ToList();
            var mesas = await (from rm in _context.Reserva_Mesas
                               join m in _context.Mesas on rm.id_mesa equals m.id_mesa
                               where ids.Contains(rm.id_reserva)
                               select new { rm.id_reserva, m.mesa_desc }).ToListAsync();

            return Json(reservas.Select(r => new
            {
                id = r.id_reserva,
                title = $"{r.res_cliente} · {r.res_personas} pers.",
                start = r.res_inicio.ToString("yyyy-MM-ddTHH:mm:ss"),
                end = r.res_fin.ToString("yyyy-MM-ddTHH:mm:ss"),
                color = Color(r.res_estado),
                textColor = r.res_estado == EstadoReserva.Pendiente ? "#3d2a05" : "#ffffff",
                editable = EstadoReserva.Ocupa(r.res_estado),
                extendedProps = new
                {
                    estado = EstadoReserva.Nombre(r.res_estado),
                    tipo = TipoReserva.Nombre(r.res_tipo),
                    telefono = r.res_telefono,
                    mesas = string.Join(", ", mesas.Where(m => m.id_reserva == r.id_reserva).Select(m => m.mesa_desc)),
                    notas = r.res_notas
                }
            }));
        }

        public async Task<IActionResult> Create(DateTime? inicio)
        {
            var empresa = await _tickets.ObtenerEmpresaAsync();
            var ahora = Fechas.AhoraPeru();
            var desde = inicio ?? ahora.Date.AddHours(Math.Max(ahora.Hour + 1, 12));
            var reserva = new MDReserva
            {
                res_inicio = desde,
                res_fin = desde.AddMinutes(empresa.gene_reserva_minutos),
                res_personas = 2,
                res_estado = EstadoReserva.Confirmada
            };
            return await Formulario(reserva, Array.Empty<int>());
        }

        public async Task<IActionResult> Edit(int id)
        {
            var reserva = await _context.Reservas.AsNoTracking().FirstOrDefaultAsync(r => r.id_reserva == id);
            if (reserva == null)
            {
                return NotFound();
            }
            return await Formulario(reserva, (await _reservas.MesasDeAsync(id)).ToArray());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guardar(
            [Bind("id_reserva,res_cliente,res_telefono,res_email,res_emails_extra,res_inicio,res_fin,res_personas,res_tipo,res_estado,res_notas")] MDReserva reserva,
            int[] mesas)
        {
            if (!ModelState.IsValid)
            {
                return await Formulario(reserva, mesas);
            }
            var r = await _reservas.GuardarAsync(reserva, mesas, SessionHelper.GetNameIdentifier(User));
            if (!r.Ok)
            {
                ModelState.AddModelError("", r.Error);
                return await Formulario(reserva, mesas);
            }
            TempData["success"] = $"Reserva de {r.Reserva.res_cliente} guardada para el {r.Reserva.res_inicio:dd/MM HH:mm}.";
            if (r.Aviso != null) TempData["error"] = r.Aviso;
            return RedirectToAction(nameof(Index), new { fecha = r.Reserva.res_inicio.ToString("yyyy-MM-dd") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Estado(int id, byte estado)
        {
            if (estado > EstadoReserva.Completada)
            {
                return BadRequest();
            }
            var r = await _reservas.CambiarEstadoAsync(id, estado);
            if (!r.Ok) TempData["error"] = r.Error;
            else
            {
                TempData["success"] = $"Reserva marcada como {EstadoReserva.Nombre(estado).ToLowerInvariant()}.";
                if (r.Aviso != null) TempData["error"] = r.Aviso;
            }
            return RedirectToAction(nameof(Edit), new { id });
        }

        // POST: Reservas/Mover - arrastrar o estirar el evento en el calendario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Mover(int id, DateTime inicio, DateTime fin)
        {
            var r = await _reservas.MoverAsync(id, inicio, fin);
            return Json(new { ok = r.Ok, mensaje = r.Error ?? r.Aviso });
        }

        // GET: Reservas/Ocupadas - mesas tomadas por otras reservas en ese horario
        public async Task<IActionResult> Ocupadas(DateTime inicio, DateTime fin, int excluir = 0)
        {
            return Json(fin > inicio ? await _reservas.MesasOcupadasAsync(inicio, fin, excluir) : new List<int>());
        }

        private async Task<IActionResult> Formulario(MDReserva reserva, int[] mesas)
        {
            ViewBag.plano = await HttpContext.RequestServices.GetRequiredService<PlanoService>().ObtenerAsync(null, conEstado: false);
            ViewBag.mesas_sel = mesas ?? Array.Empty<int>();
            ViewBag.ocupadas = reserva.res_fin > reserva.res_inicio
                ? await _reservas.MesasOcupadasAsync(reserva.res_inicio, reserva.res_fin, reserva.id_reserva)
                : new List<int>();
            ViewBag.correos = reserva.id_reserva == 0 ? new List<MDCorreo_Log>()
                : await _context.Correos.AsNoTracking().Where(c => c.id_reserva == reserva.id_reserva).OrderByDescending(c => c.id_correo).ToListAsync();
            ViewBag.correo_configurado = HttpContext.RequestServices.GetRequiredService<CorreoService>().Opciones.Configurado;
            return View("Formulario", reserva);
        }

        private static string Color(byte estado) => estado switch
        {
            EstadoReserva.Pendiente => "#f2a541",
            EstadoReserva.Confirmada => "#2e8b57",
            EstadoReserva.Cancelada => "#a39789",
            EstadoReserva.NoAsistio => "#b42318",
            EstadoReserva.Completada => "#3a312b",
            _ => "#7a6e64"
        };
    }
}
