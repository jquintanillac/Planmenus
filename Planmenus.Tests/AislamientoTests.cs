using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Services;

namespace Planmenus.Tests
{
    /// <summary>
    /// Multiempresa: una empresa nunca ve, edita ni elimina los datos de otra.
    /// Crea una empresa temporal, prueba contra la empresa 1 y la elimina al terminar.
    /// </summary>
    [Collection("App")]
    public class AislamientoTests
    {
        private readonly AppFactory _app;

        public AislamientoTests(AppFactory app)
        {
            _app = app;
        }

        private async Task<(MDEmpresa empresa, int mesa, int menu, int venta)> CrearEmpresaBAsync(string marca)
        {
            using var scope = _app.Services.CreateScope();
            var r = await scope.ServiceProvider.GetRequiredService<EmpresaService>().CrearAsync(
                new EmpresaService.Alta(marca + " Restaurante B", marca.ToLowerInvariant(), null, marca.ToLowerInvariant() + "-admin", "Admin B", "ClaveSegura123"));
            Assert.True(r.Ok, r.Error);
            var datos = await _app.Con(async db =>
            {
                var mesa = new MDMesas { mesa_desc = marca + " Mesa B", mesa_nro = 1, mesa_act = true, mesa_capacidad = 4, id_zona = await db.Zonas.Select(z => (int?)z.id_zona).FirstAsync() };
                var menu = new MDMenu { menu_desc = marca + " Plato B", menu_precio = 30m, menu_fecha = Fechas.HoyPeru().AddHours(12) };
                db.AddRange(mesa, menu);
                await db.SaveChangesAsync();
                var pago = await db.Tipo_Pagos.Select(t => t.id_tipago).FirstAsync();
                var caja = new MDCaja { id_tipago = pago, caja_monto = 30m, caja_fecha = Fechas.AhoraPeru(), caja_fecha_pago = Fechas.AhoraPeru(), caja_act = true, caja_pagado = true, caja_obs = marca };
                db.Cajas.Add(caja);
                await db.SaveChangesAsync();
                db.Caja_Detalles.Add(new MDCaja_Detalle { id_caja = caja.id_caja, id_menu = menu.id_menu, det_cant = 1, det_precio = 30m, det_subtotal = 30m });
                await db.SaveChangesAsync();
                return (mesa.id_mesa, menu.id_menu, caja.id_caja);
            }, r.Empresa.id_empresa);
            return (r.Empresa, datos.Item1, datos.Item2, datos.Item3);
        }

        /// <summary>Elimina todos los datos de la empresa temporal (en orden para respetar las llaves foraneas)</summary>
        private Task EliminarEmpresaAsync(int id) => EliminarEmpresaAsync(_app, id);

        internal static Task EliminarEmpresaAsync(AppFactory app, int id) => app.Con(async db =>
        {
            async Task Borrar<T>() where T : class, IEmpresa
            {
                await db.Set<T>().IgnoreQueryFilters().Where(x => x.id_empresa == id).ExecuteDeleteAsync();
            }
            await Borrar<MDCaja_Detalle>(); await Borrar<MDComprobante_Detalle>(); await Borrar<MDComprobante>(); await Borrar<MDImpresion_Cola>();
            await Borrar<MDCaja>(); await Borrar<MDEventos>(); await Borrar<MDReserva_Mesa>(); await Borrar<MDReserva>();
            await Borrar<MDAsignacion_Mozo>(); await Borrar<MDMenu>(); await Borrar<MDMesas>(); await Borrar<MDZona>();
            await Borrar<MDRol_Usuario>(); await Borrar<MDRol_Permisos>(); await Borrar<MDUsuarios>(); await Borrar<MDRol>();
            await Borrar<MDTipo_pago>(); await Borrar<MDGenerales>(); await Borrar<MDTicketera>(); await Borrar<MDCorreo_Log>();
            await Borrar<MDTurno_Caja>(); await Borrar<MDEgresos>(); await Borrar<MDIngresos>(); await Borrar<MDInicial>();
            await Borrar<MDDelivery>(); await Borrar<MDSerie>(); await Borrar<MDFacturacion>(); await Borrar<MDPago_Suscripcion>();
            await db.Empresas.Where(e => e.id_empresa == id).ExecuteDeleteAsync();
        });

        [Fact]
        public async Task Alta_de_empresa_crea_lo_necesario_para_trabajar()
        {
            var marca = Marca.Nueva();
            var (b, _, _, _) = await CrearEmpresaBAsync(marca);
            try
            {
                await _app.Con(async db =>
                {
                    Assert.Equal(1, await db.Generales.CountAsync());
                    Assert.Equal(4, await db.Tipo_Pagos.CountAsync());
                    Assert.Equal(1, await db.Zonas.CountAsync());
                    var rol = await db.Rols.SingleAsync();
                    Assert.Equal("Administrador", rol.rol_desc);
                    Assert.Equal(await db.Submenus.CountAsync(), await db.Rol_Permisos.CountAsync(p => p.id_rol == rol.id_rol && p.rolper_act));
                    var admin = await db.Usuarios.SingleAsync();
                    Assert.True(HashHelper.CheckHash("ClaveSegura123", admin.Usua_pass, admin.Usua_Hash));
                }, b.id_empresa);

                // El nombre de usuario es unico en todas las empresas
                using var scope = _app.Services.CreateScope();
                var otra = await scope.ServiceProvider.GetRequiredService<EmpresaService>().CrearAsync(new EmpresaService.Alta("Otra", "otra-" + marca.ToLowerInvariant(), null, marca.ToLowerInvariant() + "-admin", "X", "ClaveSegura123"));
                Assert.False(otra.Ok);
            }
            finally
            {
                await EliminarEmpresaAsync(b.id_empresa);
            }
        }

        [Fact]
        public async Task Una_empresa_no_ve_ni_modifica_datos_de_otra()
        {
            var marca = Marca.Nueva();
            var (b, mesaB, menuB, ventaB) = await CrearEmpresaBAsync(marca);
            try
            {
                var a = _app.Cliente(); // empresa 1

                // Listados y reportes de la empresa 1 no muestran nada de B
                Assert.DoesNotContain(marca, await a.HtmlAsync("/MDMesas"));
                Assert.DoesNotContain(marca, await a.HtmlAsync("/MDMenus"));
                Assert.DoesNotContain(marca, await a.HtmlAsync("/MDCajas/Cajadiaria"));
                Assert.DoesNotContain(marca, await a.HtmlAsync("/MDCajas/Rptporplato"));
                Assert.DoesNotContain(marca, await a.HtmlAsync("/Salon"));
                Assert.DoesNotContain(marca, await a.HtmlAsync("/Home/Inicio"));

                // Abrir por id un registro de B: no existe para A
                Assert.Equal(HttpStatusCode.NotFound, (await a.Http.GetAsync($"/MDMesas/Edit/{mesaB}")).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await a.Http.GetAsync($"/MDMenus/Details/{menuB}")).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await a.Http.GetAsync($"/MDCajas/Ticket/{ventaB}")).StatusCode);
                Assert.Equal(HttpStatusCode.NotFound, (await a.Http.GetAsync($"/Crearpdf/Index?id_caja={ventaB}")).StatusCode);

                // Editar enviando el id de B (los mantenimientos antiguos usan Update sin cargar el registro): no se modifica
                var zona1 = await _app.Con(db => db.Zonas.Select(z => (int?)z.id_zona).FirstOrDefaultAsync());
                await a.PostAsync("/MDMesas/Create", $"/MDMesas/Edit/{mesaB}", ("id_mesa", mesaB), ("mesa_desc", "HACKEADA"), ("mesa_nro", 99), ("mesa_act", "true"), ("mesa_capacidad", 2), ("id_zona", zona1));
                await a.PostAsync("/MDMenus/Create", $"/MDMenus/Edit/{menuB}", ("id_menu", menuB), ("menu_desc", "HACKEADO"), ("menu_precio", "1"), ("menu_fecha", Fechas.HoyPeru().ToString("yyyy-MM-ddTHH:mm")));
                // Eliminar o anular por id
                await a.PostAsync("/MDMesas/Create", $"/MDMesas/Delete/{mesaB}", ("id", mesaB));
                await a.PostAsync("/MDCajas/Cajadiaria", "/MDCajas/Anular", ("id", ventaB), ("caja_obs", "HACKEADA"));

                await _app.Con(async db =>
                {
                    var mesa = await db.Mesas.AsNoTracking().SingleAsync(m => m.id_mesa == mesaB);
                    Assert.Equal(marca + " Mesa B", mesa.mesa_desc);
                    Assert.Equal(marca + " Plato B", await db.Menus.Where(m => m.id_menu == menuB).Select(m => m.menu_desc).SingleAsync());
                    var venta = await db.Cajas.AsNoTracking().SingleAsync(c => c.id_caja == ventaB);
                    Assert.True(venta.caja_act);
                    Assert.Equal(marca, venta.caja_obs);
                }, b.id_empresa);

                // Y la empresa B si ve sus datos, pero no los de la empresa 1
                var cb = _app.ClienteDe(b.id_empresa);
                Assert.Contains(marca + " Mesa B", await cb.HtmlAsync("/MDMesas"));
                Assert.Contains(marca + " Plato B", await cb.HtmlAsync("/MDCajas/Rptporplato"));
                var mesaDeA = await _app.Con(db => db.Mesas.Select(m => (int?)m.id_mesa).FirstOrDefaultAsync());
                if (mesaDeA != null)
                {
                    Assert.Equal(HttpStatusCode.NotFound, (await cb.Http.GetAsync($"/MDMesas/Edit/{mesaDeA}")).StatusCode);
                }
                Assert.DoesNotContain("Restaurante Demo", await cb.HtmlAsync("/Configuracion"));
            }
            finally
            {
                await EliminarEmpresaAsync(b.id_empresa);
            }
        }

        [Fact]
        public async Task Registros_nuevos_quedan_en_la_empresa_del_usuario()
        {
            var marca = Marca.Nueva();
            var (b, _, _, _) = await CrearEmpresaBAsync(marca);
            try
            {
                var cb = _app.ClienteDe(b.id_empresa);
                await cb.PostOkAsync("/MDTipo_pago/Create", "/MDTipo_pago/Create", ("tipago_desc", marca + " Pago"), ("tipago_act", "true"));
                var empresaDelPago = await _app.Con(db => db.Tipo_Pagos.IgnoreQueryFilters().Where(t => t.tipago_desc == marca + " Pago").Select(t => t.id_empresa).SingleAsync());
                Assert.Equal(b.id_empresa, empresaDelPago);
                Assert.False(await _app.Con(db => db.Tipo_Pagos.AnyAsync(t => t.tipago_desc == marca + " Pago")), "La empresa 1 no debe verlo");
            }
            finally
            {
                await EliminarEmpresaAsync(b.id_empresa);
            }
        }

        [Fact]
        public async Task Carta_publica_y_agente_de_impresion_son_de_cada_empresa()
        {
            var marca = Marca.Nueva();
            var (b, _, _, ventaB) = await CrearEmpresaBAsync(marca);
            try
            {
                var anonimo = _app.CreateClient();
                var carta = await (await anonimo.GetAsync("/c/" + b.emp_codigo)).Content.ReadAsStringAsync();
                Assert.Contains(marca + " Plato B", carta);
                Assert.DoesNotContain("Lomo saltado", carta);
                Assert.Equal(HttpStatusCode.NotFound, (await anonimo.GetAsync("/c/no-existe-" + marca.ToLowerInvariant())).StatusCode);

                // Clave del agente de B: solo entrega trabajos de B
                var clave = "clave-" + Guid.NewGuid().ToString("N");
                await _app.Con(async db =>
                {
                    db.Ticketeras.Add(new MDTicketera { tk_modo = ModoImpresion.Agente, tk_impresora = "XP-80", tk_agente_clave_hash = Planmenus.Controllers.AgenteController.Hash(clave) });
                    db.Impresiones.Add(new MDImpresion_Cola { imp_creado = Fechas.AhoraPeru(), imp_tipo = "prueba-" + marca, id_caja = ventaB, imp_impresora = "XP-80", imp_datos = new byte[] { 0x1B, 0x40 } });
                    await db.SaveChangesAsync();
                }, b.id_empresa);
                var req = new HttpRequestMessage(HttpMethod.Get, "/api/agente/trabajos?max=20");
                req.Headers.Add("X-Agente-Clave", clave);
                var trabajos = await (await anonimo.SendAsync(req)).Content.ReadAsStringAsync();
                Assert.Contains("prueba-" + marca, trabajos);
                Assert.Single(System.Text.RegularExpressions.Regex.Matches(trabajos, "\"id\":"));
            }
            finally
            {
                await EliminarEmpresaAsync(b.id_empresa);
            }
        }

        [Fact]
        public async Task Sin_empresa_no_se_puede_guardar()
        {
            using var scope = _app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Datacontext>(); // sin empresa establecida
            Assert.Empty(await db.Mesas.ToListAsync());
            db.Mesas.Add(new MDMesas { mesa_desc = "Sin empresa", mesa_nro = 1 });
            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
    }
}
