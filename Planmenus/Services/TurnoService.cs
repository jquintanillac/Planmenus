using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;

namespace Planmenus.Services
{
    /// <summary>
    /// Apertura y cierre de caja con arqueo del efectivo.
    /// Efectivo esperado = fondo inicial + ventas en efectivo + ingresos - egresos del turno.
    /// </summary>
    public class TurnoService
    {
        private readonly Datacontext _context;

        public TurnoService(Datacontext context)
        {
            _context = context;
        }

        public class Resumen
        {
            public MDTurno_Caja turno { get; set; }
            public DateTime hasta { get; set; }
            public List<FilaPago> por_tipo_pago { get; set; } = new();
            public decimal ventas_total => por_tipo_pago.Sum(p => p.monto);
            public decimal ventas_efectivo => por_tipo_pago.Where(p => p.efectivo).Sum(p => p.monto);
            public decimal ingresos { get; set; }
            public decimal egresos { get; set; }
            public int pedidos_abiertos { get; set; }
            public decimal esperado => turno.turno_monto_inicial + ventas_efectivo + ingresos - egresos;
        }

        public class FilaPago
        {
            public string tipo { get; set; }
            public bool efectivo { get; set; }
            public int tickets { get; set; }
            public decimal monto { get; set; }
        }

        public Task<MDTurno_Caja> AbiertoAsync() =>
            _context.Turnos.Where(t => t.turno_abierto).OrderByDescending(t => t.id_turno).FirstOrDefaultAsync();

        public async Task<string> AbrirAsync(decimal fondo, string usuario)
        {
            if (fondo < 0)
            {
                return "El fondo inicial no puede ser negativo.";
            }
            if (await AbiertoAsync() != null)
            {
                return "Ya hay una caja abierta. Ciérrela antes de abrir otra.";
            }
            _context.Turnos.Add(new MDTurno_Caja
            {
                turno_apertura = Fechas.AhoraPeru(),
                turno_monto_inicial = fondo,
                turno_usuario_apertura = usuario,
                turno_abierto = true
            });
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<string> CerrarAsync(int id_turno, decimal contado, string obs, string usuario)
        {
            var turno = await _context.Turnos.FirstOrDefaultAsync(t => t.id_turno == id_turno && t.turno_abierto);
            if (turno == null)
            {
                return "El turno no existe o ya fue cerrado.";
            }
            if (contado < 0)
            {
                return "El efectivo contado no puede ser negativo.";
            }

            var ahora = Fechas.AhoraPeru();
            var resumen = await ResumenAsync(turno, ahora);
            turno.turno_cierre = ahora;
            turno.turno_usuario_cierre = usuario;
            turno.turno_esperado = resumen.esperado;
            turno.turno_contado = contado;
            turno.turno_diferencia = contado - resumen.esperado;
            turno.turno_obs = obs;
            turno.turno_abierto = false;
            await _context.SaveChangesAsync();
            return null;
        }

        public async Task<Resumen> ResumenAsync(MDTurno_Caja turno, DateTime? hasta = null)
        {
            var desde = turno.turno_apertura;
            var fin = hasta ?? turno.turno_cierre ?? Fechas.AhoraPeru();

            var porTipo = await (from c in _context.Cajas
                                 join t in _context.Tipo_Pagos on c.id_tipago equals t.id_tipago
                                 where c.caja_act && c.caja_pagado && c.caja_fecha_pago >= desde && c.caja_fecha_pago <= fin
                                 group c by new { t.tipago_desc, t.tipago_efectivo } into g
                                 select new FilaPago
                                 {
                                     tipo = g.Key.tipago_desc,
                                     efectivo = g.Key.tipago_efectivo,
                                     tickets = g.Count(),
                                     monto = g.Sum(x => x.caja_monto)
                                 }).OrderByDescending(f => f.monto).ToListAsync();

            // Ingresos y egresos: los registrados solo con fecha (sin hora) cuentan para el dia completo
            var finDia = fin.Date.AddDays(1);
            var ingresos = await _context.Ingresos.Where(i => i.ingre_fecha >= desde.Date && i.ingre_fecha < finDia).ToListAsync();
            var egresos = await _context.Egresos.Where(e => e.egre_fecha >= desde.Date && e.egre_fecha < finDia).ToListAsync();

            return new Resumen
            {
                turno = turno,
                hasta = fin,
                por_tipo_pago = porTipo,
                ingresos = ingresos.Where(i => EnTurno(i.ingre_fecha, desde, fin)).Sum(i => i.ingre_monto),
                egresos = egresos.Where(e => EnTurno(e.egre_fecha, desde, fin)).Sum(e => e.egre_costo),
                pedidos_abiertos = await _context.Cajas.CountAsync(c => c.caja_act && !c.caja_pagado)
            };
        }

        // Los formularios guardan la hora sin segundos: se compara por minuto para no perder
        // un movimiento registrado en el mismo minuto de la apertura o del cierre
        private static bool EnTurno(DateTime fecha, DateTime desde, DateTime hasta)
        {
            if (fecha.TimeOfDay == TimeSpan.Zero)
            {
                return fecha.Date >= desde.Date && fecha.Date <= hasta.Date;
            }
            var inicio = desde.AddTicks(-(desde.Ticks % TimeSpan.TicksPerMinute));
            var fin = hasta.AddTicks(-(hasta.Ticks % TimeSpan.TicksPerMinute)).AddMinutes(1);
            return fecha >= inicio && fecha < fin;
        }
    }
}
