using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using System.Globalization;
using System.Net;

namespace Planmenus.Services
{
    /// <summary>
    /// Reglas de las reservas: horarios sin cruces por mesa, capacidad y avisos por correo.
    /// </summary>
    public class ReservaService
    {
        private static readonly CultureInfo Peru = CultureInfo.GetCultureInfo("es-PE");
        private readonly Datacontext _context;
        private readonly CorreoService _correo;
        private readonly TicketService _tickets;

        public ReservaService(Datacontext context, CorreoService correo, TicketService tickets)
        {
            _context = context;
            _correo = correo;
            _tickets = tickets;
        }

        public record Resultado(MDReserva Reserva, string Error, string Aviso = null)
        {
            public bool Ok => Error == null;
        }

        public async Task<Resultado> GuardarAsync(MDReserva datos, int[] mesas, string usuario)
        {
            mesas = (mesas ?? Array.Empty<int>()).Distinct().ToArray();
            if (datos.res_fin <= datos.res_inicio)
            {
                return new Resultado(null, "La hora de fin debe ser posterior a la de inicio.");
            }
            if (datos.id_reserva == 0 && datos.res_inicio < Fechas.AhoraPeru().AddMinutes(-30))
            {
                return new Resultado(null, "No se puede registrar una reserva en el pasado.");
            }

            var cruce = await CruceAsync(mesas, datos.res_inicio, datos.res_fin, datos.id_reserva);
            if (cruce != null)
            {
                return new Resultado(null, cruce);
            }

            MDReserva reserva;
            bool cambioHorario = false;
            if (datos.id_reserva == 0)
            {
                reserva = new MDReserva { res_creado = Fechas.AhoraPeru(), res_usuario = usuario };
                _context.Reservas.Add(reserva);
            }
            else
            {
                reserva = await _context.Reservas.FirstOrDefaultAsync(r => r.id_reserva == datos.id_reserva);
                if (reserva == null)
                {
                    return new Resultado(null, "La reserva no existe.");
                }
                cambioHorario = reserva.res_inicio != datos.res_inicio || reserva.res_fin != datos.res_fin;
            }

            var estadoAnterior = reserva.id_reserva == 0 ? (byte?)null : reserva.res_estado;
            reserva.res_cliente = datos.res_cliente?.Trim();
            reserva.res_telefono = datos.res_telefono?.Trim();
            reserva.res_email = datos.res_email?.Trim();
            reserva.res_emails_extra = datos.res_emails_extra?.Trim();
            reserva.res_inicio = datos.res_inicio;
            reserva.res_fin = datos.res_fin;
            reserva.res_personas = datos.res_personas;
            reserva.res_tipo = datos.res_tipo;
            reserva.res_estado = datos.res_estado;
            reserva.res_notas = datos.res_notas?.Trim();
            if (cambioHorario)
            {
                // Hay que volver a avisar con el nuevo horario
                reserva.res_recordatorio_enviado = null;
                reserva.res_confirmacion_enviada = null;
            }
            await _context.SaveChangesAsync();

            var actuales = await _context.Reserva_Mesas.Where(m => m.id_reserva == reserva.id_reserva).ToListAsync();
            _context.Reserva_Mesas.RemoveRange(actuales.Where(a => !mesas.Contains(a.id_mesa)));
            _context.Reserva_Mesas.AddRange(mesas.Where(m => !actuales.Any(a => a.id_mesa == m))
                .Select(m => new MDReserva_Mesa { id_reserva = reserva.id_reserva, id_mesa = m }));
            await _context.SaveChangesAsync();

            string aviso = null;
            if (mesas.Length > 0)
            {
                var capacidad = await _context.Mesas.Where(m => mesas.Contains(m.id_mesa)).SumAsync(m => m.mesa_capacidad);
                if (capacidad < reserva.res_personas)
                {
                    aviso = $"Las mesas elegidas tienen capacidad para {capacidad} y la reserva es para {reserva.res_personas}.";
                }
            }

            var errorCorreo = await AvisarCambioEstadoAsync(reserva, estadoAnterior);
            return new Resultado(reserva, null, string.Join(" ", new[] { aviso, errorCorreo }.Where(s => s != null)).NullSiVacio());
        }

        public async Task<Resultado> CambiarEstadoAsync(int id, byte estado)
        {
            var reserva = await _context.Reservas.FirstOrDefaultAsync(r => r.id_reserva == id);
            if (reserva == null)
            {
                return new Resultado(null, "La reserva no existe.");
            }
            if (EstadoReserva.Ocupa(estado) && !EstadoReserva.Ocupa(reserva.res_estado))
            {
                var mesas = await MesasDeAsync(id);
                var cruce = await CruceAsync(mesas, reserva.res_inicio, reserva.res_fin, id);
                if (cruce != null)
                {
                    return new Resultado(reserva, cruce);
                }
            }
            var anterior = reserva.res_estado;
            reserva.res_estado = estado;
            await _context.SaveChangesAsync();
            var errorCorreo = await AvisarCambioEstadoAsync(reserva, anterior);
            return new Resultado(reserva, null, errorCorreo);
        }

        /// <summary>Cambio de horario desde el calendario (arrastrar o estirar el evento)</summary>
        public async Task<Resultado> MoverAsync(int id, DateTime inicio, DateTime fin)
        {
            var reserva = await _context.Reservas.AsNoTracking().FirstOrDefaultAsync(r => r.id_reserva == id);
            if (reserva == null)
            {
                return new Resultado(null, "La reserva no existe.");
            }
            reserva.res_inicio = inicio;
            reserva.res_fin = fin;
            return await GuardarAsync(reserva, (await MesasDeAsync(id)).ToArray(), reserva.res_usuario);
        }

        public Task<List<int>> MesasDeAsync(int id_reserva) =>
            _context.Reserva_Mesas.Where(m => m.id_reserva == id_reserva).Select(m => m.id_mesa).ToListAsync();

        /// <summary>Mesas ocupadas por otras reservas en ese horario</summary>
        public Task<List<int>> MesasOcupadasAsync(DateTime inicio, DateTime fin, int excluir = 0) =>
            (from rm in _context.Reserva_Mesas
             join r in _context.Reservas on rm.id_reserva equals r.id_reserva
             where r.id_reserva != excluir
                   && (r.res_estado == EstadoReserva.Pendiente || r.res_estado == EstadoReserva.Confirmada)
                   && r.res_inicio < fin && r.res_fin > inicio
             select rm.id_mesa).Distinct().ToListAsync();

        /// <summary>
        /// Envia los recordatorios de las reservas confirmadas que empiezan dentro de las horas configuradas.
        /// </summary>
        public async Task<int> EnviarRecordatoriosAsync(CancellationToken ct)
        {
            var empresa = await _tickets.ObtenerEmpresaAsync();
            var ahora = Fechas.AhoraPeru();
            var limite = ahora.AddHours(Math.Clamp(empresa.gene_recordatorio_horas, 1, 72));
            var pendientes = await _context.Reservas
                .Where(r => r.res_estado == EstadoReserva.Confirmada && r.res_recordatorio_enviado == null
                            && r.res_inicio > ahora && r.res_inicio <= limite)
                .ToListAsync(ct);

            foreach (var r in pendientes)
            {
                var error = await EnviarAsync("recordatorio", r, empresa,
                    $"Su reserva comienza pronto · {empresa.gene_empre}",
                    "¡Lo esperamos pronto!",
                    $"Le recordamos que su reserva en <strong>{Html(empresa.gene_empre)}</strong> comienza a las <strong>{r.res_inicio:HH:mm}</strong>.", ct);
                // Se marca aunque falle para no reintentar en bucle; el error queda en Correo_Log
                r.res_recordatorio_enviado = ahora;
                await _context.SaveChangesAsync(ct);
            }
            return pendientes.Count;
        }

        private async Task<string> AvisarCambioEstadoAsync(MDReserva r, byte? anterior)
        {
            var empresa = await _tickets.ObtenerEmpresaAsync();
            if (r.res_estado == EstadoReserva.Confirmada && r.res_confirmacion_enviada == null)
            {
                var error = await EnviarAsync("confirmacion", r, empresa,
                    $"Reserva confirmada · {empresa.gene_empre}",
                    "¡Su reserva está confirmada!",
                    $"Gracias por elegir <strong>{Html(empresa.gene_empre)}</strong>. Estos son los datos de su reserva:");
                r.res_confirmacion_enviada = Fechas.AhoraPeru();
                await _context.SaveChangesAsync();
                return error == null ? null : "La reserva se guardó, pero no se pudo enviar el correo de confirmación: " + error;
            }
            if (r.res_estado == EstadoReserva.Cancelada && anterior != null && anterior != EstadoReserva.Cancelada && r.res_confirmacion_enviada != null)
            {
                var error = await EnviarAsync("cancelacion", r, empresa,
                    $"Reserva cancelada · {empresa.gene_empre}",
                    "Su reserva fue cancelada",
                    "Le informamos que la siguiente reserva fue cancelada. Si es un error, comuníquese con nosotros.");
                // Si se vuelve a confirmar, el cliente debe recibir otra confirmacion y su recordatorio
                r.res_confirmacion_enviada = null;
                r.res_recordatorio_enviado = null;
                await _context.SaveChangesAsync();
                return error == null ? null : "No se pudo enviar el aviso de cancelación: " + error;
            }
            return null;
        }

        private async Task<string> EnviarAsync(string tipo, MDReserva r, MDGenerales empresa, string asunto, string titulo, string intro, CancellationToken ct = default)
        {
            var para = new[] { r.res_email, r.res_emails_extra, empresa.gene_correo_avisos };
            if (Correos.Normalizar(para).Count == 0)
            {
                return null; // Nadie a quien avisar: no es un error
            }
            var mesas = await (from rm in _context.Reserva_Mesas
                               join m in _context.Mesas on rm.id_mesa equals m.id_mesa
                               where rm.id_reserva == r.id_reserva
                               select m.mesa_desc).ToListAsync(ct);
            return await _correo.EnviarAsync(tipo, para, asunto, Plantilla(empresa, titulo, intro, r, mesas), r.id_reserva, ct);
        }

        private static string Plantilla(MDGenerales e, string titulo, string intro, MDReserva r, List<string> mesas)
        {
            string fila(string k, string v) => string.IsNullOrWhiteSpace(v) ? "" :
                $"<tr><td style=\"padding:6px 0;color:#7a6e64;width:120px\">{k}</td><td style=\"padding:6px 0;font-weight:600\">{Html(v)}</td></tr>";
            var fecha = r.res_inicio.ToString("dddd d 'de' MMMM 'de' yyyy", Peru);
            fecha = char.ToUpper(fecha[0], Peru) + fecha[1..];
            return $@"<!DOCTYPE html><html><body style=""margin:0;background:#fbf6ee;font-family:Arial,Helvetica,sans-serif;color:#2a211b"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#fbf6ee;padding:24px 12px""><tr><td align=""center"">
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:520px;background:#ffffff;border:1px solid #eadfd2;border-radius:14px;overflow:hidden"">
<tr><td style=""background:#e4572e;color:#ffffff;padding:20px 24px;font-size:20px;font-weight:bold"">{Html(e.gene_empre)}</td></tr>
<tr><td style=""padding:24px"">
<h1 style=""margin:0 0 8px;font-size:22px;color:#2a211b"">{Html(titulo)}</h1>
<p style=""margin:0 0 16px;line-height:1.5"">{intro}</p>
<table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-top:1px solid #eadfd2;border-bottom:1px solid #eadfd2;margin-bottom:16px"">
{fila("A nombre de", r.res_cliente)}{fila("Fecha", fecha)}{fila("Hora", $"{r.res_inicio:HH:mm} a {r.res_fin:HH:mm}")}
{fila("Personas", r.res_personas.ToString())}{fila("Mesa", string.Join(", ", mesas))}{fila("Tipo", TipoReserva.Nombre(r.res_tipo))}{fila("Notas", r.res_notas)}
</table>
<p style=""margin:0;color:#7a6e64;font-size:13px;line-height:1.5"">{Html(e.gene_direc)}{(string.IsNullOrWhiteSpace(e.gene_telef) ? "" : " · Tel. " + Html(e.gene_telef))}</p>
</td></tr></table>
<p style=""color:#9c8f83;font-size:12px;margin-top:12px"">Reserva N° {r.id_reserva:D5} · Este es un mensaje automático.</p>
</td></tr></table></body></html>";
        }

        private static string Html(string s) => WebUtility.HtmlEncode(s ?? "");

        private async Task<string> CruceAsync(IEnumerable<int> mesas, DateTime inicio, DateTime fin, int excluir)
        {
            var ids = mesas.ToList();
            if (ids.Count == 0)
            {
                return null;
            }
            var cruce = await (from rm in _context.Reserva_Mesas
                               join r in _context.Reservas on rm.id_reserva equals r.id_reserva
                               join m in _context.Mesas on rm.id_mesa equals m.id_mesa
                               where ids.Contains(rm.id_mesa) && r.id_reserva != excluir
                                     && (r.res_estado == EstadoReserva.Pendiente || r.res_estado == EstadoReserva.Confirmada)
                                     && r.res_inicio < fin && r.res_fin > inicio
                               select new { m.mesa_desc, r.res_cliente, r.res_inicio, r.res_fin }).FirstOrDefaultAsync();
            return cruce == null ? null
                : $"{cruce.mesa_desc} ya está reservada de {cruce.res_inicio:HH:mm} a {cruce.res_fin:HH:mm} ({cruce.res_cliente}).";
        }
    }

    internal static class TextoExtensiones
    {
        public static string NullSiVacio(this string s) => string.IsNullOrWhiteSpace(s) ? null : s;
    }
}
