using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Planmenus.Controllers;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Services;

namespace Planmenus.Tests
{
    /// <summary>
    /// Planes y suscripcion: limites por plan, prueba gratis, vencimiento, pagos y recuperacion de contraseña.
    /// Cada prueba trabaja en una empresa temporal y la elimina al terminar.
    /// </summary>
    [Collection("App")]
    public class SuscripcionTests
    {
        private const string Clave = "ClaveSegura123";
        private readonly AppFactory _app;

        public SuscripcionTests(AppFactory app)
        {
            _app = app;
        }

        private async Task<MDEmpresa> CrearAsync(string marca)
        {
            using var scope = _app.Services.CreateScope();
            var r = await scope.ServiceProvider.GetRequiredService<EmpresaService>().CrearAsync(
                new EmpresaService.Alta(marca + " Suscripcion", marca.ToLowerInvariant(), null, marca.ToLowerInvariant() + "-dueno", "Dueño", Clave, marca.ToLowerInvariant() + "@ejemplo.pe"));
            Assert.True(r.Ok, r.Error);
            return r.Empresa;
        }

        /// <summary>Cambia el vencimiento directo en la base y olvida el estado guardado por el middleware</summary>
        private async Task VenceAsync(int id, DateTime? vence)
        {
            await _app.Con(db => db.Empresas.Where(e => e.id_empresa == id).ExecuteUpdateAsync(s => s.SetProperty(e => e.emp_vence, vence)));
            _app.Services.GetRequiredService<IMemoryCache>().Remove(EstadoEmpresa.Clave(id));
        }

        private async Task<string> LoginAsync(string usuario, string clave)
        {
            var r = await _app.Anonimo().PostAsync("/Home/Index", "/Home/Login", ("Usua_user", usuario), ("Usua_pass", clave));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            return r.Headers.Location?.ToString();
        }

        [Fact]
        public async Task Plan_basico_limita_usuarios_y_mesas()
        {
            var marca = Marca.Nueva();
            var b = await CrearAsync(marca);
            try
            {
                Assert.Equal("basico", b.emp_plan);
                Assert.Equal(Fechas.HoyPeru().AddDays(14), b.emp_vence);
                var cliente = _app.ClienteDe(b.id_empresa);

                // Basico: 3 usuarios activos (el administrador ya es uno)
                for (int i = 1; i <= 3; i++)
                {
                    var r = await cliente.PostAsync("/MDUsuarios/Create", "/MDUsuarios/Create", ("Usua_user", $"{marca.ToLowerInvariant()}-u{i}"),
                        ("Usua_nombres", "Usuario " + i), ("Usua_apellidos", "Prueba"), ("Usua_email", ""), ("Usua_pass", Clave), ("Usua_act", "true"));
                    if (i < 3) Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
                    else
                    {
                        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
                        Assert.Contains("Mi suscripci", WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync()));
                    }
                }
                Assert.Equal(3, await _app.Con(db => db.Usuarios.CountAsync(), b.id_empresa));

                // Basico: 15 mesas activas
                await _app.Con(async db =>
                {
                    var zona = await db.Zonas.Select(z => (int?)z.id_zona).FirstAsync();
                    db.Mesas.AddRange(Enumerable.Range(1, 15).Select(n => new MDMesas { mesa_nro = n, mesa_desc = $"Mesa {n}", mesa_act = true, mesa_capacidad = 4, id_zona = zona }));
                    await db.SaveChangesAsync();
                }, b.id_empresa);
                await cliente.PostOkAsync("/Plano", "/Plano/NuevaMesa", ("forma", "cuadrada"));
                var form = await cliente.PostAsync("/MDMesas/Create", "/MDMesas/Create", ("mesa_desc", "Mesa 16"), ("mesa_nro", 16), ("mesa_act", "true"), ("mesa_capacidad", 4));
                Assert.Equal(HttpStatusCode.OK, form.StatusCode);
                Assert.Equal(15, await _app.Con(db => db.Mesas.CountAsync(), b.id_empresa));

                // Una mesa inactiva si se puede registrar (no cuenta para el limite)
                await cliente.PostOkAsync("/MDMesas/Create", "/MDMesas/Create", ("mesa_desc", "Mesa guardada"), ("mesa_nro", 17), ("mesa_act", "false"), ("mesa_capacidad", 4));

                // Mi suscripcion muestra el uso
                var html = WebUtility.HtmlDecode(await cliente.HtmlAsync("/Suscripcion"));
                Assert.Contains("15 de 15", html);
                Assert.Contains("3 de 3", html);
            }
            finally
            {
                await AislamientoTests.EliminarEmpresaAsync(_app, b.id_empresa);
            }
        }

        [Fact]
        public async Task Suscripcion_vencida_bloquea_hasta_registrar_el_pago()
        {
            var marca = Marca.Nueva();
            var b = await CrearAsync(marca);
            try
            {
                var cliente = _app.ClienteDe(b.id_empresa);

                // En los dias de gracia se trabaja con aviso
                await VenceAsync(b.id_empresa, Fechas.HoyPeru().AddDays(-1));
                var inicio = await cliente.Http.GetAsync("/Home/Inicio");
                Assert.Equal(HttpStatusCode.OK, inicio.StatusCode);
                Assert.Contains("venci", WebUtility.HtmlDecode(await inicio.Content.ReadAsStringAsync()));

                // Pasada la gracia solo se ve Mi suscripcion
                await VenceAsync(b.id_empresa, Fechas.HoyPeru().AddDays(-10));
                var bloqueado = await cliente.Http.GetAsync("/Home/Inicio");
                Assert.Equal(HttpStatusCode.Redirect, bloqueado.StatusCode);
                Assert.Equal("/Suscripcion", bloqueado.Headers.Location?.ToString());
                Assert.Equal(HttpStatusCode.OK, (await cliente.Http.GetAsync("/Suscripcion")).StatusCode);

                // El superadministrador registra el pago: 2 meses de Estandar desde hoy
                await _app.Superadmin().PostOkAsync("/Plataforma", "/Plataforma/Pago", ("id", b.id_empresa), ("plan", "estandar"), ("meses", 2),
                    ("monto", ""), ("medio", "Yape"), ("referencia", "OP-" + marca));
                var e = await _app.Con(db => db.Empresas.AsNoTracking().SingleAsync(x => x.id_empresa == b.id_empresa));
                Assert.Equal("estandar", e.emp_plan);
                Assert.Equal(Fechas.HoyPeru().AddMonths(2), e.emp_vence);
                var pago = await _app.Con(db => db.Pagos_Suscripcion.SingleAsync(), b.id_empresa);
                Assert.Equal(178m, pago.pago_monto);
                Assert.Equal("OP-" + marca, pago.pago_referencia);

                Assert.Equal(HttpStatusCode.OK, (await cliente.Http.GetAsync("/Home/Inicio")).StatusCode);
                Assert.Contains("OP-", await _app.Superadmin().HtmlAsync("/Plataforma"));

                // Renovar antes de vencer suma desde el vencimiento actual
                await _app.Superadmin().PostOkAsync("/Plataforma", "/Plataforma/Pago", ("id", b.id_empresa), ("plan", "estandar"), ("meses", 1),
                    ("monto", "80"), ("medio", "Transferencia"), ("referencia", ""));
                e = await _app.Con(db => db.Empresas.AsNoTracking().SingleAsync(x => x.id_empresa == b.id_empresa));
                Assert.Equal(Fechas.HoyPeru().AddMonths(2).AddMonths(1), e.emp_vence);
            }
            finally
            {
                await AislamientoTests.EliminarEmpresaAsync(_app, b.id_empresa);
            }
        }

        [Fact]
        public async Task Recuperar_contrasena_con_enlace_de_un_solo_uso()
        {
            var marca = Marca.Nueva();
            var b = await CrearAsync(marca);
            var usuario = marca.ToLowerInvariant() + "-dueno";
            try
            {
                // La respuesta es la misma exista o no el usuario
                var anonimo = _app.Anonimo();
                Assert.Equal("/", (await anonimo.PostAsync("/Recuperar", "/Recuperar", ("dato", usuario))).Headers.Location?.ToString());
                Assert.Equal("/", (await anonimo.PostAsync("/Recuperar", "/Recuperar", ("dato", "no-existe-" + marca))).Headers.Location?.ToString());

                // El enlace del correo (se arma igual que el controlador)
                var u = await _app.Con(db => db.Usuarios.AsNoTracking().SingleAsync(), b.id_empresa);
                var t = _app.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("Planmenus.Recuperar").ToTimeLimitedDataProtector()
                    .Protect($"{u.IdUsuario}|{RecuperarController.Huella(u)}", TimeSpan.FromHours(1));
                var url = "/Recuperar/Nueva?t=" + Uri.EscapeDataString(t);

                var r = await anonimo.PostAsync(url, "/Recuperar/Nueva", ("t", t), ("clave", "NuevaClave456"), ("clave2", "OtraDistinta"));
                Assert.Equal(HttpStatusCode.OK, r.StatusCode);
                r = await anonimo.PostAsync(url, "/Recuperar/Nueva", ("t", t), ("clave", "NuevaClave456"), ("clave2", "NuevaClave456"));
                Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);

                Assert.Equal("/Home/Inicio", await LoginAsync(usuario, "NuevaClave456"));
                Assert.Equal("/", await LoginAsync(usuario, Clave));

                // Ya usado: la contraseña cambio y el enlace deja de valer
                var otraVez = await anonimo.Http.GetAsync(url);
                Assert.Equal(HttpStatusCode.Redirect, otraVez.StatusCode);
                Assert.Equal("/Recuperar", otraVez.Headers.Location?.ToString());
            }
            finally
            {
                await AislamientoTests.EliminarEmpresaAsync(_app, b.id_empresa);
            }
        }
    }
}
