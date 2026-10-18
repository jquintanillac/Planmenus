using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Models;
using System.Net.Sockets;

namespace Planmenus.Services
{
    /// <summary>
    /// Envia los tickets segun el modo configurado: navegador, ticketera de red o agente local (impresora por nombre).
    /// </summary>
    public class ImpresionService
    {
        private readonly Datacontext _context;
        private readonly TicketService _tickets;
        private readonly EscPosService _escpos;
        private readonly ILogger<ImpresionService> _logger;

        public ImpresionService(Datacontext context, TicketService tickets, EscPosService escpos, ILogger<ImpresionService> logger)
        {
            _context = context;
            _tickets = tickets;
            _escpos = escpos;
            _logger = logger;
        }

        public record Resultado(bool Ok, string Mensaje, bool Navegador = false);

        /// <summary>El agente se considera conectado si reporto en los ultimos 2 minutos</summary>
        public static bool AgenteConectado(MDTicketera f) =>
            f.tk_agente_contacto != null && Fechas.AhoraPeru() - f.tk_agente_contacto.Value < TimeSpan.FromMinutes(2);

        /// <summary>
        /// Imprime el ticket de una venta. Al cobrar puede abrir la gaveta si el pago es en efectivo.
        /// </summary>
        public async Task<Resultado> ImprimirVentaAsync(int id_caja, bool alCobrar)
        {
            var ticket = await _tickets.ObtenerAsync(id_caja);
            if (ticket == null)
            {
                return new Resultado(false, "El pedido no existe.");
            }
            if (alCobrar && !ticket.formato.tk_auto)
            {
                return new Resultado(true, null, Navegador: ticket.formato.tk_modo == ModoImpresion.Navegador);
            }
            bool gaveta = false;
            if (alCobrar && ticket.formato.tk_gaveta && ticket.pagado)
            {
                gaveta = await (from c in _context.Cajas
                                join t in _context.Tipo_Pagos on c.id_tipago equals t.id_tipago
                                where c.id_caja == id_caja
                                select t.tipago_efectivo).FirstOrDefaultAsync();
            }
            return await ImprimirAsync(ticket, gaveta, ticket.pagado ? "ticket" : "precuenta", id_caja);
        }

        public async Task<Resultado> ImprimirAsync(VMTicket ticket, bool abrirGaveta, string tipo, int? id_caja = null)
        {
            var f = ticket.formato;
            switch (f.tk_modo)
            {
                case ModoImpresion.Red:
                    if (string.IsNullOrWhiteSpace(f.tk_ip))
                    {
                        return new Resultado(false, "Falta la IP de la ticketera en Configuración → Ticketera.");
                    }
                    try
                    {
                        await _escpos.ImprimirAsync(ticket, f.tk_ip, f.tk_puerto, abrirGaveta);
                        return new Resultado(true, $"Enviado a la ticketera {f.tk_ip}.");
                    }
                    catch (Exception ex) when (ex is SocketException or OperationCanceledException or IOException)
                    {
                        _logger.LogWarning(ex, "No se pudo imprimir en {Ip}:{Puerto}", f.tk_ip, f.tk_puerto);
                        return new Resultado(false, $"No se pudo conectar con la ticketera {f.tk_ip}:{f.tk_puerto}. Revise que esté encendida y en la misma red.");
                    }

                case ModoImpresion.Agente:
                    if (string.IsNullOrWhiteSpace(f.tk_impresora))
                    {
                        return new Resultado(false, "Elija la impresora en Configuración → Ticketera.");
                    }
                    _context.Impresiones.Add(new MDImpresion_Cola
                    {
                        imp_creado = Fechas.AhoraPeru(),
                        imp_tipo = tipo,
                        id_caja = id_caja,
                        imp_impresora = f.tk_impresora,
                        imp_datos = _escpos.Generar(ticket, abrirGaveta),
                        imp_estado = EstadoImpresion.Pendiente
                    });
                    await _context.SaveChangesAsync();
                    return AgenteConectado(f)
                        ? new Resultado(true, $"Enviado a {f.tk_impresora}.")
                        : new Resultado(true, $"En cola para {f.tk_impresora}. El agente de impresión no está conectado: se imprimirá cuando se conecte.");

                default:
                    return new Resultado(true, null, Navegador: true);
            }
        }
    }
}
