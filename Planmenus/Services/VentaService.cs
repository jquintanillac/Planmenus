using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Models;

namespace Planmenus.Services
{
    /// <summary>
    /// Reglas de los pedidos: crear, agregar o quitar platos y cobrar.
    /// Los precios siempre se toman del menu del dia guardado en la base de datos.
    /// </summary>
    public class VentaService
    {
        private readonly Datacontext _context;

        public VentaService(Datacontext context)
        {
            _context = context;
        }

        public record Resultado(MDCaja Caja, string Error)
        {
            public bool Ok => Error == null;
        }

        /// <summary>
        /// Registra un pedido. Si es para cocina y la mesa ya tiene un pedido abierto, los platos se agregan a ese pedido.
        /// </summary>
        public async Task<Resultado> RegistrarAsync(VMVenta venta, string usuario)
        {
            var (detalle, error) = await ArmarDetalleAsync(venta.items);
            if (error != null)
            {
                return new Resultado(null, error);
            }

            if (venta.id_caja > 0)
            {
                return await AgregarAsync(venta.id_caja.Value, detalle);
            }

            int? mesa = venta.id_mesa > 0 ? venta.id_mesa : null;
            if (!venta.Cobrar && mesa != null)
            {
                var abierto = await PedidoAbiertoDeMesaAsync(mesa.Value);
                if (abierto != null)
                {
                    return await AgregarAsync(abierto.id_caja, detalle);
                }
            }

            if (venta.Cobrar && !await TipoPagoValidoAsync(venta.id_tipago))
            {
                return new Resultado(null, "Seleccione un tipo de pago válido.");
            }

            var ahora = Fechas.AhoraPeru();
            var caja = new MDCaja
            {
                id_tipago = venta.Cobrar ? venta.id_tipago : null,
                id_mesa = mesa,
                id_deliv = venta.id_deliv > 0 ? venta.id_deliv : null,
                caja_obs = venta.caja_obs,
                caja_monto = detalle.Sum(d => d.det_subtotal),
                caja_fecha = ahora,
                caja_usuario = usuario,
                caja_act = true,
                caja_pagado = venta.Cobrar,
                caja_fecha_pago = venta.Cobrar ? ahora : null,
                caja_cocina = EstadoCocina.EnCocina
            };

            await using var tx = await _context.Database.BeginTransactionAsync();
            _context.Cajas.Add(caja);
            await _context.SaveChangesAsync();
            detalle.ForEach(d => d.id_caja = caja.id_caja);
            _context.Caja_Detalles.AddRange(detalle);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return new Resultado(caja, null);
        }

        public async Task<Resultado> CobrarAsync(int id_caja, int? id_tipago)
        {
            var caja = await PedidoAbiertoAsync(id_caja);
            if (caja == null)
            {
                return new Resultado(null, "El pedido no existe o ya fue cobrado.");
            }
            if (!await TipoPagoValidoAsync(id_tipago))
            {
                return new Resultado(caja, "Seleccione un tipo de pago válido.");
            }
            if (caja.caja_monto <= 0)
            {
                return new Resultado(caja, "El pedido no tiene platos.");
            }

            caja.id_tipago = id_tipago;
            caja.caja_pagado = true;
            caja.caja_fecha_pago = Fechas.AhoraPeru();
            await _context.SaveChangesAsync();
            return new Resultado(caja, null);
        }

        public async Task<Resultado> QuitarPlatoAsync(int id_caja, int id_detalle)
        {
            var caja = await PedidoAbiertoAsync(id_caja);
            if (caja == null)
            {
                return new Resultado(null, "Solo se pueden quitar platos de un pedido abierto.");
            }
            var det = await _context.Caja_Detalles.FirstOrDefaultAsync(d => d.id_detalle == id_detalle && d.id_caja == id_caja);
            if (det != null)
            {
                _context.Caja_Detalles.Remove(det);
                await _context.SaveChangesAsync();
                caja.caja_monto = await _context.Caja_Detalles.Where(d => d.id_caja == id_caja).SumAsync(d => (decimal?)d.det_subtotal) ?? 0;
                await _context.SaveChangesAsync();
            }
            return new Resultado(caja, null);
        }

        public Task<MDCaja> PedidoAbiertoAsync(int id_caja) =>
            _context.Cajas.FirstOrDefaultAsync(c => c.id_caja == id_caja && c.caja_act && !c.caja_pagado);

        public Task<MDCaja> PedidoAbiertoDeMesaAsync(int id_mesa) =>
            _context.Cajas.OrderByDescending(c => c.id_caja).FirstOrDefaultAsync(c => c.id_mesa == id_mesa && c.caja_act && !c.caja_pagado);

        private async Task<Resultado> AgregarAsync(int id_caja, List<MDCaja_Detalle> nuevos)
        {
            var caja = await PedidoAbiertoAsync(id_caja);
            if (caja == null)
            {
                return new Resultado(null, "El pedido ya fue cobrado o anulado; registre uno nuevo.");
            }

            var actuales = await _context.Caja_Detalles.Where(d => d.id_caja == id_caja).ToListAsync();
            foreach (var n in nuevos)
            {
                var existente = actuales.FirstOrDefault(d => d.id_menu == n.id_menu && d.det_precio == n.det_precio);
                if (existente != null)
                {
                    existente.det_cant += n.det_cant;
                    existente.det_subtotal = existente.det_precio * existente.det_cant;
                }
                else
                {
                    n.id_caja = id_caja;
                    _context.Caja_Detalles.Add(n);
                    actuales.Add(n);
                }
            }
            caja.caja_monto = actuales.Sum(d => d.det_subtotal);
            // Hay platos nuevos: el pedido vuelve a cocina
            caja.caja_cocina = EstadoCocina.EnCocina;
            await _context.SaveChangesAsync();
            return new Resultado(caja, null);
        }

        private async Task<(List<MDCaja_Detalle>, string)> ArmarDetalleAsync(List<VMVentaItem> items)
        {
            var agrupados = (items ?? new())
                .Where(i => i.cantidad > 0)
                .GroupBy(i => i.id_menu)
                .Select(g => new { id_menu = g.Key, cantidad = g.Sum(i => i.cantidad) })
                .ToList();
            if (agrupados.Count == 0 || agrupados.Any(i => i.cantidad > 999))
            {
                return (null, "El pedido no tiene platos válidos.");
            }

            var hoy = Fechas.HoyPeru();
            var ids = agrupados.Select(i => i.id_menu).ToList();
            var precios = await _context.Menus
                .Where(m => ids.Contains(m.id_menu) && m.menu_fecha.Date == hoy)
                .ToDictionaryAsync(m => m.id_menu, m => m.menu_precio);
            if (precios.Count != ids.Count)
            {
                return (null, "El pedido contiene platos que no están en el menú de hoy.");
            }

            return (agrupados.Select(i => new MDCaja_Detalle
            {
                id_menu = i.id_menu,
                det_cant = i.cantidad,
                det_precio = precios[i.id_menu],
                det_subtotal = precios[i.id_menu] * i.cantidad
            }).ToList(), null);
        }

        private async Task<bool> TipoPagoValidoAsync(int? id_tipago) =>
            id_tipago > 0 && await _context.Tipo_Pagos.AnyAsync(t => t.id_tipago == id_tipago && t.tipago_act);
    }
}
