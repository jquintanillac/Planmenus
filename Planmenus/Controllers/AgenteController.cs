using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using System.Security.Cryptography;
using System.Text;

namespace Planmenus.Controllers
{
    /// <summary>
    /// API para el Agente de impresion instalado en la PC del local. Se autentica con la cabecera
    /// X-Agente-Clave (la clave se genera en Configuracion → Ticketera).
    /// </summary>
    [AllowAnonymous]
    [ApiController]
    [Route("api/agente")]
    public class AgenteController : ControllerBase
    {
        private static readonly TimeSpan ReintentoTrabajoTomado = TimeSpan.FromMinutes(2);
        private const int IntentosMaximos = 5;
        private readonly Datacontext _context;

        public AgenteController(Datacontext context)
        {
            _context = context;
        }

        public static string Hash(string clave) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clave ?? "")));

        public class Estado
        {
            public string equipo { get; set; }
            public string version { get; set; }
            public List<string> impresoras { get; set; } = new();
        }

        // POST api/agente/estado - el agente informa que esta vivo y que impresoras tiene instaladas
        [HttpPost("estado")]
        public async Task<IActionResult> ReportarEstado([FromBody] Estado estado)
        {
            var f = await AutenticarAsync();
            if (f == null)
            {
                return Unauthorized(new { error = "Clave del agente inválida. Genere una nueva en Configuración → Ticketera." });
            }
            f.tk_agente_contacto = Fechas.AhoraPeru();
            f.tk_agente_equipo = Recortar(estado?.equipo, 100);
            var lista = string.Join('|', (estado?.impresoras ?? new()).Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Replace("|", " ").Trim()).Distinct());
            f.tk_agente_impresoras = Recortar(lista, 2000);
            await _context.SaveChangesAsync();
            return Ok(new { impresora = f.tk_impresora, modo = f.tk_modo });
        }

        // GET api/agente/trabajos - trabajos pendientes; quedan "tomados" hasta que el agente informe el resultado
        [HttpGet("trabajos")]
        public async Task<IActionResult> Trabajos(int max = 5)
        {
            var f = await AutenticarAsync();
            if (f == null)
            {
                return Unauthorized(new { error = "Clave del agente inválida." });
            }
            var ahora = Fechas.AhoraPeru();
            f.tk_agente_contacto = ahora;

            // Limpieza de trabajos viejos
            var limite = ahora.AddDays(-7);
            await _context.Impresiones.Where(t => t.imp_creado < limite).ExecuteDeleteAsync();

            var vencido = ahora - ReintentoTrabajoTomado;
            var trabajos = await _context.Impresiones
                .Where(t => t.imp_estado == EstadoImpresion.Pendiente && (t.imp_tomado == null || t.imp_tomado < vencido) && t.imp_intentos < IntentosMaximos)
                .OrderBy(t => t.id_trabajo)
                .Take(Math.Clamp(max, 1, 20))
                .ToListAsync();
            foreach (var t in trabajos)
            {
                t.imp_tomado = ahora;
                t.imp_intentos++;
            }
            await _context.SaveChangesAsync();
            return Ok(trabajos.Select(t => new { id = t.id_trabajo, impresora = t.imp_impresora, tipo = t.imp_tipo, datos = Convert.ToBase64String(t.imp_datos) }));
        }

        public class ResultadoTrabajo
        {
            public bool ok { get; set; }
            public string error { get; set; }
        }

        // POST api/agente/trabajos/{id} - resultado de la impresion
        [HttpPost("trabajos/{id:int}")]
        public async Task<IActionResult> Resultado(int id, [FromBody] ResultadoTrabajo r)
        {
            var f = await AutenticarAsync();
            if (f == null)
            {
                return Unauthorized(new { error = "Clave del agente inválida." });
            }
            var t = await _context.Impresiones.FirstOrDefaultAsync(x => x.id_trabajo == id);
            if (t == null)
            {
                return NotFound();
            }
            t.imp_terminado = Fechas.AhoraPeru();
            if (r?.ok == true)
            {
                t.imp_estado = EstadoImpresion.Impreso;
                t.imp_error = null;
            }
            else
            {
                // Se reintenta hasta el maximo de intentos; luego queda con error para revisarlo
                t.imp_error = Recortar(r?.error ?? "Error desconocido", 500);
                t.imp_estado = t.imp_intentos >= IntentosMaximos ? EstadoImpresion.Error : EstadoImpresion.Pendiente;
                t.imp_tomado = null;
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        private async Task<MDTicketera> AutenticarAsync()
        {
            var clave = Request.Headers["X-Agente-Clave"].ToString();
            if (string.IsNullOrWhiteSpace(clave))
            {
                return null;
            }
            var f = await _context.Ticketeras.OrderBy(t => t.id_ticketera).FirstOrDefaultAsync();
            if (f?.tk_agente_clave_hash == null)
            {
                return null;
            }
            var esperado = Encoding.ASCII.GetBytes(f.tk_agente_clave_hash);
            var recibido = Encoding.ASCII.GetBytes(Hash(clave));
            return CryptographicOperations.FixedTimeEquals(esperado, recibido) ? f : null;
        }

        private static string Recortar(string s, int max) => s == null || s.Length <= max ? s : s[..max];
    }
}
