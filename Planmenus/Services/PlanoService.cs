using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;

namespace Planmenus.Services
{
    /// <summary>
    /// Arma el plano del salon: mesas por zona con su estado (libre, ocupada, lista, reservada) y mozo.
    /// </summary>
    public class PlanoService
    {
        /// <summary>Una mesa libre se muestra como reservada desde estos minutos antes de la reserva</summary>
        public const int MinutosAvisoReserva = 90;

        private readonly Datacontext _context;

        public PlanoService(Datacontext context)
        {
            _context = context;
        }

        public class Plano
        {
            public DateTime ahora { get; set; }
            public List<MDZona> zonas { get; set; } = new();
            public int? zona_actual { get; set; }
            public List<Mesa> mesas { get; set; } = new();
            public IEnumerable<Mesa> MesasDeZona => mesas.Where(m => m.zona == zona_actual || (zona_actual == null && m.zona == null));
        }

        public class Mesa
        {
            public int id { get; set; }
            public int nro { get; set; }
            public string desc { get; set; }
            public int cap { get; set; }
            public string forma { get; set; }
            public int? x { get; set; }
            public int? y { get; set; }
            public int w { get; set; }
            public int h { get; set; }
            public int rot { get; set; }
            public int? zona { get; set; }
            public bool activa { get; set; }

            /// <summary>libre, ocupada, listo, reservada</summary>
            public string estado { get; set; } = "libre";
            public int? id_caja { get; set; }
            public decimal monto { get; set; }
            public DateTime? desde { get; set; }
            public int platos { get; set; }
            public string mozo { get; set; }
            public string mozo_usuario { get; set; }
            public DateTime? reserva_hora { get; set; }
            public string reserva_cliente { get; set; }
            public int reserva_personas { get; set; }
        }

        public async Task<Plano> ObtenerAsync(int? zona, bool conEstado, bool incluirInactivas = false)
        {
            var ahora = Fechas.AhoraPeru();
            var zonas = await _context.Zonas.AsNoTracking().Where(z => z.zona_act).OrderBy(z => z.zona_orden).ThenBy(z => z.zona_desc).ToListAsync();
            var zonaActual = zona != null && zonas.Any(z => z.id_zona == zona) ? zona : zonas.FirstOrDefault()?.id_zona;

            var mesas = await _context.Mesas.AsNoTracking()
                .Where(m => incluirInactivas || m.mesa_act)
                .OrderBy(m => m.mesa_nro)
                .Select(m => new Mesa
                {
                    id = m.id_mesa,
                    nro = m.mesa_nro,
                    desc = m.mesa_desc,
                    cap = m.mesa_capacidad,
                    forma = m.mesa_forma ?? FormaMesa.Cuadrada,
                    x = m.mesa_x,
                    y = m.mesa_y,
                    w = m.mesa_ancho,
                    h = m.mesa_alto,
                    rot = m.mesa_rotacion,
                    zona = m.id_zona,
                    activa = m.mesa_act
                }).ToListAsync();

            // Mesas de zonas eliminadas o sin zona: se muestran en la primera zona
            var idsZonas = zonas.Select(z => z.id_zona).ToHashSet();
            foreach (var m in mesas.Where(m => m.zona == null || !idsZonas.Contains(m.zona.Value)))
            {
                m.zona = zonas.FirstOrDefault()?.id_zona;
            }

            if (conEstado)
            {
                await CargarEstadoAsync(mesas, ahora);
            }
            return new Plano { ahora = ahora, zonas = zonas, zona_actual = zonaActual, mesas = mesas };
        }

        private async Task CargarEstadoAsync(List<Mesa> mesas, DateTime ahora)
        {
            var abiertos = await _context.Cajas.AsNoTracking()
                .Where(c => c.caja_act && !c.caja_pagado && c.id_mesa != null)
                .Select(c => new
                {
                    c.id_caja,
                    c.id_mesa,
                    c.caja_monto,
                    c.caja_fecha,
                    c.caja_cocina,
                    c.caja_mozo,
                    platos = _context.Caja_Detalles.Where(d => d.id_caja == c.id_caja).Sum(d => (int?)d.det_cant) ?? 0
                }).ToListAsync();

            var hoy = ahora.Date;
            var asignados = await (from a in _context.Asignaciones
                                   join u in _context.Usuarios on a.IdUsuario equals u.IdUsuario
                                   where a.asig_fecha == hoy
                                   select new { a.id_mesa, u.Usua_user, u.Usua_nombres }).ToListAsync();

            var limite = ahora.AddMinutes(MinutosAvisoReserva);
            var reservas = await (from rm in _context.Reserva_Mesas
                                  join r in _context.Reservas on rm.id_reserva equals r.id_reserva
                                  where (r.res_estado == EstadoReserva.Confirmada || r.res_estado == EstadoReserva.Pendiente)
                                        && r.res_inicio <= limite && r.res_fin > ahora
                                  orderby r.res_inicio
                                  select new { rm.id_mesa, r.res_inicio, r.res_cliente, r.res_personas }).ToListAsync();

            var nombres = await _context.Usuarios.AsNoTracking().ToDictionaryAsync(u => u.Usua_user, u => u.Usua_nombres);

            foreach (var m in mesas)
            {
                var asig = asignados.FirstOrDefault(a => a.id_mesa == m.id);
                m.mozo_usuario = asig?.Usua_user;
                m.mozo = asig?.Usua_nombres;

                var res = reservas.FirstOrDefault(r => r.id_mesa == m.id);
                if (res != null)
                {
                    m.reserva_hora = res.res_inicio;
                    m.reserva_cliente = res.res_cliente;
                    m.reserva_personas = res.res_personas;
                }

                var pedido = abiertos.Where(a => a.id_mesa == m.id).MaxBy(a => a.id_caja);
                if (pedido != null)
                {
                    m.estado = pedido.caja_cocina == EstadoCocina.Listo ? "listo" : "ocupada";
                    m.id_caja = pedido.id_caja;
                    m.monto = pedido.caja_monto;
                    m.desde = pedido.caja_fecha;
                    m.platos = pedido.platos;
                    if (!string.IsNullOrEmpty(pedido.caja_mozo))
                    {
                        m.mozo_usuario = pedido.caja_mozo;
                        m.mozo = nombres.GetValueOrDefault(pedido.caja_mozo, pedido.caja_mozo);
                    }
                }
                else if (res != null)
                {
                    m.estado = "reservada";
                }
            }
        }

        /// <summary>Usuario del mozo asignado hoy a la mesa (o null)</summary>
        public Task<string> MozoAsignadoAsync(int id_mesa)
        {
            var hoy = Fechas.HoyPeru();
            return (from a in _context.Asignaciones
                    join u in _context.Usuarios on a.IdUsuario equals u.IdUsuario
                    where a.asig_fecha == hoy && a.id_mesa == id_mesa
                    select u.Usua_user).FirstOrDefaultAsync();
        }

        /// <summary>Usuarios activos cuyo rol puede tomar pedidos o atender el salon</summary>
        public async Task<List<MDUsuarios>> MozosAsync()
        {
            var mozos = await (from u in _context.Usuarios
                               join ru in _context.Rol_Usuarios on u.IdUsuario equals ru.IdUsuario
                               join r in _context.Rols on ru.id_rol equals r.id_rol
                               where u.Usua_act && r.rol_act
                                     && _context.Rol_Permisos.Any(p => p.id_rol == r.id_rol && p.rolper_act && (p.id_submenu == 18 || p.id_submenu == 25))
                               select u).Distinct().ToListAsync();
            return mozos.OrderBy(u => u.Usua_nombres).ToList();
        }
    }
}
