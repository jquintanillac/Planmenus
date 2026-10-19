using Microsoft.EntityFrameworkCore;
using Planmenus.Data.Entities;
using Planmenus.Helpers;

namespace Planmenus.Tests
{
    /// <summary>
    /// Ventas, mesas, cocina, reservas, turnos de caja y mozos: registrar, modificar y anular/eliminar.
    /// </summary>
    [Collection("App")]
    public class OperacionTests
    {
        private readonly AppFactory _app;

        public OperacionTests(AppFactory app)
        {
            _app = app;
        }

        private static string Fecha(DateTime d) => d.ToString("yyyy-MM-ddTHH:mm");

        private async Task<(int menu, int mesa)> CrearMenuYMesaAsync(string marca) => await _app.Con(async db =>
        {
            var menu = new MDMenu { menu_desc = marca + " Lomo", menu_precio = 20m, menu_fecha = Fechas.HoyPeru().AddHours(12) };
            var mesa = new MDMesas { mesa_desc = marca + " Mesa", mesa_nro = 950, mesa_act = true, mesa_capacidad = 4, id_zona = await db.Zonas.Where(z => z.zona_act).Select(z => (int?)z.id_zona).FirstOrDefaultAsync() };
            db.AddRange(menu, mesa);
            await db.SaveChangesAsync();
            return (menu.id_menu, mesa.id_mesa);
        });

        private Task LimpiarAsync(int menu, int mesa) => _app.Con(async db =>
        {
            var cajas = db.Cajas.Where(c => db.Caja_Detalles.Any(d => d.id_caja == c.id_caja && d.id_menu == menu) || c.id_mesa == mesa);
            db.Caja_Detalles.RemoveRange(db.Caja_Detalles.Where(d => cajas.Any(c => c.id_caja == d.id_caja)));
            db.Impresiones.RemoveRange(db.Impresiones.Where(i => cajas.Any(c => c.id_caja == i.id_caja)));
            db.Cajas.RemoveRange(cajas);
            await db.SaveChangesAsync();
            db.Asignaciones.RemoveRange(db.Asignaciones.Where(a => a.id_mesa == mesa));
            db.Eventos.RemoveRange(db.Eventos.Where(e => e.id_menu == menu));
            db.Menus.RemoveRange(db.Menus.Where(m => m.id_menu == menu));
            db.Mesas.RemoveRange(db.Mesas.Where(m => m.id_mesa == mesa));
            await db.SaveChangesAsync();
        });

        [Fact]
        public async Task Venta_registrar_anular_y_eliminar()
        {
            var c = _app.Cliente();
            var marca = Marca.Nueva();
            var (menu, mesa) = await CrearMenuYMesaAsync(marca);
            try
            {
                var tipo = await _app.Con(db => db.Tipo_Pagos.Where(t => t.tipago_act).Select(t => t.id_tipago).FirstAsync());
                // El precio enviado se ignora: el total sale de la base (2 x 20 = 40)
                var destino = await c.PostOkAsync("/MDCajas/Create", "/MDCajas/Create",
                    ("accion", "cobrar"), ("id_tipago", tipo), ("items[0].id_menu", menu), ("items[0].cantidad", 2), ("caja_monto", 1), ("caja_obs", marca), ("comp_tipo", "00"));
                Assert.Contains("/MDCajas/Ticket/", destino);
                var venta = await _app.Con(db => db.Cajas.AsNoTracking().SingleAsync(x => x.caja_obs == marca));
                Assert.Equal((40m, true, true), (venta.caja_monto, venta.caja_pagado, venta.caja_act));
                await c.HtmlAsync($"/MDCajas/Ticket/{venta.id_caja}");

                await c.PostOkAsync($"/MDCajas/Edit/{venta.id_caja}", "/MDCajas/Anular", ("id", venta.id_caja), ("caja_obs", marca + " anulada"));
                Assert.False(await _app.Con(db => db.Cajas.Where(x => x.id_caja == venta.id_caja).Select(x => x.caja_act).SingleAsync()));

                await c.PostOkAsync($"/MDCajas/Delete/{venta.id_caja}", $"/MDCajas/Delete/{venta.id_caja}", ("id", venta.id_caja));
                Assert.False(await _app.Con(db => db.Cajas.AnyAsync(x => x.id_caja == venta.id_caja)));
            }
            finally
            {
                await LimpiarAsync(menu, mesa);
            }
        }

        [Fact]
        public async Task Pedido_en_mesa_agregar_quitar_cocina_y_cobrar()
        {
            var c = _app.Cliente();
            var marca = Marca.Nueva();
            var (menu, mesa) = await CrearMenuYMesaAsync(marca);
            try
            {
                await c.PostOkAsync($"/MDCajas/Create?id_mesa={mesa}", "/MDCajas/Create", ("accion", "cocina"), ("id_mesa", mesa), ("items[0].id_menu", menu), ("items[0].cantidad", 1));
                await c.PostOkAsync($"/MDCajas/Create?id_mesa={mesa}", "/MDCajas/Create", ("accion", "cocina"), ("id_mesa", mesa), ("items[0].id_menu", menu), ("items[0].cantidad", 2));
                var pedido = await _app.Con(db => db.Cajas.AsNoTracking().SingleAsync(x => x.id_mesa == mesa));
                Assert.Equal((60m, false), (pedido.caja_monto, pedido.caja_pagado)); // se sumo al mismo pedido

                var salon = await c.HtmlAsync("/Salon");
                Assert.Contains("estado-ocupada", salon);

                await c.PostOkAsync("/Cocina", "/Cocina/Estado", ("id", pedido.id_caja), ("estado", EstadoCocina.Listo));
                Assert.Equal(EstadoCocina.Listo, await _app.Con(db => db.Cajas.Where(x => x.id_caja == pedido.id_caja).Select(x => x.caja_cocina).SingleAsync()));

                var det = await _app.Con(db => db.Caja_Detalles.Where(d => d.id_caja == pedido.id_caja).Select(d => d.id_detalle).SingleAsync());
                await c.PostOkAsync($"/Salon/Pedido/{pedido.id_caja}", "/Salon/Quitar", ("id", pedido.id_caja), ("id_detalle", det));
                Assert.Equal(0m, await _app.Con(db => db.Cajas.Where(x => x.id_caja == pedido.id_caja).Select(x => x.caja_monto).SingleAsync()));

                await c.PostOkAsync($"/MDCajas/Create?id_caja={pedido.id_caja}", "/MDCajas/Create", ("accion", "cocina"), ("id_caja", pedido.id_caja), ("items[0].id_menu", menu), ("items[0].cantidad", 1));
                var tipo = await _app.Con(db => db.Tipo_Pagos.Where(t => t.tipago_act).Select(t => t.id_tipago).FirstAsync());
                await c.PostOkAsync($"/Salon/Pedido/{pedido.id_caja}", "/Salon/Cobrar", ("id", pedido.id_caja), ("id_tipago", tipo), ("comp_tipo", "00"));
                var cobrado = await _app.Con(db => db.Cajas.AsNoTracking().SingleAsync(x => x.id_caja == pedido.id_caja));
                Assert.Equal((20m, true), (cobrado.caja_monto, cobrado.caja_pagado));
            }
            finally
            {
                await LimpiarAsync(menu, mesa);
            }
        }

        [Fact]
        public async Task Reserva_registrar_editar_mover_y_cancelar()
        {
            var c = _app.Cliente();
            var marca = Marca.Nueva();
            var (menu, mesa) = await CrearMenuYMesaAsync(marca);
            try
            {
                var inicio = Fechas.HoyPeru().AddDays(3).AddHours(20);
                await c.PostOkAsync("/Reservas/Create", "/Reservas/Guardar",
                    ("id_reserva", 0), ("res_cliente", marca), ("res_telefono", "999888777"), ("res_inicio", Fecha(inicio)), ("res_fin", Fecha(inicio.AddHours(2))),
                    ("res_personas", 4), ("res_tipo", TipoReserva.Mesa), ("res_estado", EstadoReserva.Pendiente), ("mesas", mesa));
                var r = await _app.Con(db => db.Reservas.AsNoTracking().SingleAsync(x => x.res_cliente == marca));
                Assert.Equal(new List<int> { mesa }, await _app.Con(db => db.Reserva_Mesas.Where(m => m.id_reserva == r.id_reserva).Select(m => m.id_mesa).ToListAsync()));

                // Otra reserva en la misma mesa y horario no se permite
                var cruce = await c.PostAsync("/Reservas/Create", "/Reservas/Guardar",
                    ("id_reserva", 0), ("res_cliente", marca + "-X"), ("res_inicio", Fecha(inicio.AddMinutes(30))), ("res_fin", Fecha(inicio.AddHours(3))),
                    ("res_personas", 2), ("res_tipo", 0), ("res_estado", 0), ("mesas", mesa));
                Assert.Equal(System.Net.HttpStatusCode.OK, cruce.StatusCode);
                Assert.False(await _app.Con(db => db.Reservas.AnyAsync(x => x.res_cliente == marca + "-X")));

                await c.PostOkAsync($"/Reservas/Edit/{r.id_reserva}", "/Reservas/Guardar",
                    ("id_reserva", r.id_reserva), ("res_cliente", marca), ("res_inicio", Fecha(inicio)), ("res_fin", Fecha(inicio.AddHours(2))),
                    ("res_personas", 6), ("res_tipo", TipoReserva.Evento), ("res_estado", EstadoReserva.Pendiente), ("res_notas", "Cumpleaños"), ("mesas", mesa));
                Assert.Equal(6, await _app.Con(db => db.Reservas.Where(x => x.id_reserva == r.id_reserva).Select(x => x.res_personas).SingleAsync()));

                var mover = await c.PostAsync("/Reservas", "/Reservas/Mover", ("id", r.id_reserva), ("inicio", Fecha(inicio.AddHours(1))), ("fin", Fecha(inicio.AddHours(3))));
                Assert.Contains("\"ok\":true", await mover.Content.ReadAsStringAsync());

                var eventos = await c.HtmlAsync($"/Reservas/Eventos?start={inicio.Date:yyyy-MM-dd}T00:00:00-05:00&end={inicio.Date.AddDays(1):yyyy-MM-dd}T00:00:00-05:00");
                Assert.Contains(marca, eventos);

                await c.PostOkAsync($"/Reservas/Edit/{r.id_reserva}", "/Reservas/Estado", ("id", r.id_reserva), ("estado", EstadoReserva.Cancelada));
                Assert.Equal(EstadoReserva.Cancelada, await _app.Con(db => db.Reservas.Where(x => x.id_reserva == r.id_reserva).Select(x => x.res_estado).SingleAsync()));
            }
            finally
            {
                await _app.Con(async db =>
                {
                    var ids = db.Reservas.Where(x => x.res_cliente.StartsWith(marca)).Select(x => x.id_reserva);
                    db.Correos.RemoveRange(db.Correos.Where(x => ids.Contains(x.id_reserva.Value)));
                    db.Reserva_Mesas.RemoveRange(db.Reserva_Mesas.Where(x => ids.Contains(x.id_reserva)));
                    db.Reservas.RemoveRange(db.Reservas.Where(x => x.res_cliente.StartsWith(marca)));
                    await db.SaveChangesAsync();
                });
                await LimpiarAsync(menu, mesa);
            }
        }

        [Fact]
        public async Task Turno_de_caja_abrir_y_cerrar_con_arqueo()
        {
            if (await _app.Con(db => db.Turnos.AnyAsync(t => t.turno_abierto)))
            {
                return; // hay una caja abierta de verdad: no se toca
            }
            var c = _app.Cliente();
            int? id = null;
            try
            {
                await c.PostOkAsync("/Turnos", "/Turnos/Abrir", ("fondo", "50"));
                var turno = await _app.Con(db => db.Turnos.AsNoTracking().SingleAsync(t => t.turno_abierto));
                id = turno.id_turno;
                Assert.Equal(50m, turno.turno_monto_inicial);

                await c.PostOkAsync("/Turnos", "/Turnos/Abrir", ("fondo", "10"));  // no se puede abrir otra
                Assert.Equal(1, await _app.Con(db => db.Turnos.CountAsync(t => t.turno_abierto)));

                await c.PostOkAsync("/Turnos", "/Turnos/Cerrar", ("id", turno.id_turno), ("contado", "48"), ("obs", "TEST faltan 2"));
                var cerrado = await _app.Con(db => db.Turnos.AsNoTracking().SingleAsync(t => t.id_turno == turno.id_turno));
                Assert.False(cerrado.turno_abierto);
                Assert.Equal(48m - cerrado.turno_esperado, cerrado.turno_diferencia);
                await c.HtmlAsync($"/Turnos/Detalle/{turno.id_turno}");
            }
            finally
            {
                if (id != null) await _app.Con(async db => { db.Turnos.Remove(await db.Turnos.FindAsync(id.Value)); await db.SaveChangesAsync(); });
            }
        }

        [Fact]
        public async Task Planificacion_de_mozos_asignar_cambiar_y_quitar()
        {
            var c = _app.Cliente();
            var marca = Marca.Nueva();
            var (menu, mesa) = await CrearMenuYMesaAsync(marca);
            var mozo = await _app.Con(db => db.Usuarios.Where(u => u.Usua_user == "admin").Select(u => u.IdUsuario).SingleAsync());
            var dia = Fechas.HoyPeru().AddDays(30).ToString("yyyy-MM-dd");
            try
            {
                // Se conservan las asignaciones existentes de las demas mesas
                var otras = await _app.Con(db => db.Asignaciones.Where(a => a.asig_fecha == DateTime.Parse(dia)).Select(a => new { a.id_mesa, a.IdUsuario }).ToListAsync());
                var campos = otras.Select(o => ($"asig[{o.id_mesa}]", (object)o.IdUsuario)).Append(($"asig[{mesa}]", mozo)).Append(("fecha", dia)).ToArray();
                await c.PostOkAsync($"/Mozos?fecha={dia}", "/Mozos/Guardar", campos);
                Assert.True(await _app.Con(db => db.Asignaciones.AnyAsync(a => a.id_mesa == mesa && a.IdUsuario == mozo)));

                var sinMozo = otras.Select(o => ($"asig[{o.id_mesa}]", (object)o.IdUsuario)).Append(($"asig[{mesa}]", "")).Append(("fecha", dia)).ToArray();
                await c.PostOkAsync($"/Mozos?fecha={dia}", "/Mozos/Guardar", sinMozo);
                Assert.False(await _app.Con(db => db.Asignaciones.AnyAsync(a => a.id_mesa == mesa)));
            }
            finally
            {
                await LimpiarAsync(menu, mesa);
            }
        }

        [Theory]
        [InlineData("/Home/Inicio")]
        [InlineData("/Configuracion")]
        [InlineData("/Ticketera")]
        [InlineData("/Facturacion")]
        [InlineData("/Comprobantes")]
        [InlineData("/MDUsuarios")]
        [InlineData("/MDRols")]
        [InlineData("/MDRol_Usuario")]
        [InlineData("/MDRol_Permisos")]
        [InlineData("/MDMenuprins")]
        [InlineData("/MDSubmenus")]
        [InlineData("/MDMenus")]
        [InlineData("/MDMenus/Calendar")]
        [InlineData("/MDTipo_pago")]
        [InlineData("/MDDeliveries")]
        [InlineData("/MDMesas")]
        [InlineData("/MDEgresos")]
        [InlineData("/MDIngresos")]
        [InlineData("/MDInicials")]
        [InlineData("/MDCajas/Create")]
        [InlineData("/MDCajas/Cajadiaria")]
        [InlineData("/MDCajas/Index")]
        [InlineData("/MDCajas/RptListpagos")]
        [InlineData("/MDCajas/Rpttipodepago")]
        [InlineData("/MDCajas/Rptporplato")]
        [InlineData("/MDCajas/Rptcantplato")]
        [InlineData("/Salon")]
        [InlineData("/Cocina")]
        [InlineData("/Reservas")]
        [InlineData("/Mozos")]
        [InlineData("/Turnos")]
        [InlineData("/Plano")]
        [InlineData("/Carta/Qr")]
        public async Task Cada_pantalla_carga_y_tiene_ayuda(string url)
        {
            var html = await _app.Cliente().HtmlAsync(url);
            Assert.Contains("id=\"ayudaBtn\"", html);
        }

        [Fact]
        public async Task La_carta_digital_es_publica()
        {
            var r = await _app.CreateClient().GetAsync("/Carta");
            Assert.Equal(System.Net.HttpStatusCode.OK, r.StatusCode);
        }
    }
}
