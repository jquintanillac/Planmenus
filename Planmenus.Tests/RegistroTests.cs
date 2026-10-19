using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planmenus.Data.Entities;
using Planmenus.Services;

namespace Planmenus.Tests
{
    /// <summary>
    /// Registro publico de restaurantes, asistente inicial y panel de la plataforma (superadministrador).
    /// Cada prueba elimina la empresa que crea.
    /// </summary>
    [Collection("App")]
    public class RegistroTests
    {
        private const string Clave = "ClaveSegura123";
        private readonly AppFactory _app;

        public RegistroTests(AppFactory app)
        {
            _app = app;
        }

        private static (string, object)[] Formulario(string marca, string codigo = null) => new (string, object)[]
        {
            ("nombre", marca + " Restaurante"), ("codigo", codigo ?? marca.ToLowerInvariant()), ("ruc", ""),
            ("admin_nombre", "Dueña " + marca), ("email", marca.ToLowerInvariant() + "@ejemplo.pe"),
            ("usuario", marca.ToLowerInvariant() + "-dueno"), ("clave", Clave), ("clave2", Clave), ("acepto", "true"), ("sitio_web", "")
        };

        private Task<MDEmpresa> EmpresaAsync(string codigo) => _app.Con(db => db.Empresas.AsNoTracking().FirstOrDefaultAsync(e => e.emp_codigo == codigo));

        private async Task<string> LoginAsync(string usuario)
        {
            var r = await _app.Anonimo().PostAsync("/Home/Index", "/Home/Login", ("Usua_user", usuario), ("Usua_pass", Clave));
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            return r.Headers.Location?.ToString();
        }

        [Fact]
        public async Task Registro_crea_la_empresa_inicia_sesion_y_el_asistente_la_prepara()
        {
            var marca = Marca.Nueva();
            var codigo = marca.ToLowerInvariant();
            var r = await _app.Anonimo().PostAsync("/Registro", "/Registro", Formulario(marca));
            MDEmpresa empresa = null;
            try
            {
                Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
                Assert.Equal("/Registro/Bienvenida", r.Headers.Location?.ToString());
                Assert.Contains(r.Headers.GetValues("Set-Cookie"), c => c.StartsWith(".AspNetCore.Cookies"));

                empresa = await EmpresaAsync(codigo);
                Assert.NotNull(empresa);
                Assert.True(empresa.emp_activa);
                var admin = await _app.Con(db => db.Usuarios.AsNoTracking().SingleAsync(), empresa.id_empresa);
                Assert.Equal(codigo + "-dueno", admin.Usua_user);
                Assert.False(admin.Usua_superadmin);
                Assert.NotNull(admin.Usua_ultimo_ingreso);

                // Asistente: 3 mesas y 1 plato (el que no tiene precio se ignora)
                var cliente = _app.ClienteDe(empresa.id_empresa);
                await cliente.PostOkAsync("/Registro/Bienvenida", "/Registro/Bienvenida",
                    ("direccion", "Av. Prueba 123"), ("telefono", "999888777"), ("mesas", 3), ("capacidad", 6),
                    ("platos[0].nombre", marca + " Ají de gallina"), ("platos[0].precio", "18.50"),
                    ("platos[1].nombre", marca + " Sin precio"), ("platos[1].precio", ""));
                await _app.Con(async db =>
                {
                    Assert.Equal("Av. Prueba 123", (await db.Generales.SingleAsync()).gene_direc);
                    var mesas = await db.Mesas.OrderBy(m => m.mesa_nro).ToListAsync();
                    Assert.Equal(new[] { 1, 2, 3 }, mesas.Select(m => m.mesa_nro));
                    Assert.All(mesas, m => { Assert.Equal(6, m.mesa_capacidad); Assert.NotNull(m.id_zona); Assert.NotNull(m.mesa_x); });
                    var plato = await db.Menus.SingleAsync();
                    Assert.Equal(18.50m, plato.menu_precio);
                    Assert.Equal(1, await db.Eventos.CountAsync(e => e.id_menu == plato.id_menu));
                }, empresa.id_empresa);

                // Su carta publica y su login funcionan
                Assert.Contains(marca + " Ají de gallina", System.Net.WebUtility.HtmlDecode(await _app.Anonimo().HtmlAsync("/c/" + codigo)));
                Assert.Equal("/Home/Inicio", await LoginAsync(codigo + "-dueno"));
            }
            finally
            {
                empresa ??= await EmpresaAsync(codigo);
                if (empresa != null) await AislamientoTests.EliminarEmpresaAsync(_app, empresa.id_empresa);
            }
        }

        [Fact]
        public async Task Registro_rechaza_codigo_en_uso_datos_invalidos_y_robots()
        {
            var marca = Marca.Nueva();
            var anonimo = _app.Anonimo();

            // La direccion de la carta ya existe
            var r = await anonimo.PostAsync("/Registro", "/Registro", Formulario(marca, "principal"));
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains("en uso", await r.Content.ReadAsStringAsync());
            Assert.Null(await _app.Con(db => db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Usua_user == marca.ToLowerInvariant() + "-dueno")));

            // Disponibilidad mientras se escribe
            Assert.Contains("\"disponible\":false", await anonimo.HtmlAsync("/Registro/Disponible?codigo=Principal"));
            Assert.Contains("\"disponible\":true", await anonimo.HtmlAsync("/Registro/Disponible?codigo=" + marca));

            // Sin aceptar los terminos y con claves distintas
            var campos = Formulario(marca).Select(c => c.Item1 switch { "acepto" => ("acepto", (object)"false"), "clave2" => ("clave2", (object)"OtraClave999"), _ => c }).ToArray();
            r = await anonimo.PostAsync("/Registro", "/Registro", campos);
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Null(await EmpresaAsync(marca.ToLowerInvariant()));

            // Campo trampa lleno: se responde normal pero no se crea nada
            campos = Formulario(marca).Select(c => c.Item1 == "sitio_web" ? ("sitio_web", (object)"http://spam.example") : c).ToArray();
            r = await anonimo.PostAsync("/Registro", "/Registro", campos);
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Null(await EmpresaAsync(marca.ToLowerInvariant()));
        }

        [Fact]
        public async Task Empresa_suspendida_no_ingresa_ni_muestra_su_carta_y_se_puede_reactivar()
        {
            var marca = Marca.Nueva();
            EmpresaService.Resultado alta;
            using (var scope = _app.Services.CreateScope())
            {
                alta = await scope.ServiceProvider.GetRequiredService<EmpresaService>().CrearAsync(
                    new EmpresaService.Alta(marca + " Suspendida", marca.ToLowerInvariant(), null, marca.ToLowerInvariant() + "-dueno", "Dueño", Clave));
            }
            Assert.True(alta.Ok, alta.Error);
            var id = alta.Empresa.id_empresa;
            try
            {
                var admin = _app.Superadmin();
                await admin.PostOkAsync("/Plataforma", "/Plataforma/Estado", ("id", id), ("activa", "false"));
                Assert.False((await EmpresaAsync(marca.ToLowerInvariant())).emp_activa);

                // Vuelve a la pantalla de ingreso ("/") con el aviso de suspension
                Assert.Equal("/", await LoginAsync(marca.ToLowerInvariant() + "-dueno"));
                Assert.Equal(HttpStatusCode.NotFound, (await _app.Anonimo().Http.GetAsync("/c/" + marca.ToLowerInvariant())).StatusCode);
                // Una sesion que ya estaba abierta tambien se cierra
                var abierta = await _app.ClienteDe(id).Http.GetAsync("/Home/Inicio");
                Assert.Equal(HttpStatusCode.Redirect, abierta.StatusCode);
                Assert.Equal("/Home/Index", abierta.Headers.Location?.ToString());

                await admin.PostOkAsync("/Plataforma", "/Plataforma/Estado", ("id", id), ("activa", "true"));
                await admin.PostOkAsync("/Plataforma", "/Plataforma/Plan", ("id", id), ("plan", "premium"));
                var e = await EmpresaAsync(marca.ToLowerInvariant());
                Assert.True(e.emp_activa);
                Assert.Equal("premium", e.emp_plan);
                Assert.Equal("/Home/Inicio", await LoginAsync(marca.ToLowerInvariant() + "-dueno"));
            }
            finally
            {
                await AislamientoTests.EliminarEmpresaAsync(_app, id);
            }
        }

        [Fact]
        public async Task Plataforma_solo_para_el_superadministrador()
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await _app.Cliente().Http.GetAsync("/Plataforma")).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await _app.Anonimo().Http.GetAsync("/Plataforma")).StatusCode);

            var admin = _app.Superadmin();
            var html = await admin.HtmlAsync("/Plataforma");
            Assert.Contains("/c/principal", html);

            // No puede suspender su propia empresa
            await admin.PostOkAsync("/Plataforma", "/Plataforma/Estado", ("id", 1), ("activa", "false"));
            Assert.True((await EmpresaAsync("principal")).emp_activa);

            // Crear un restaurante desde el panel
            var marca = Marca.Nueva();
            await admin.PostOkAsync("/Plataforma", "/Plataforma/Crear", ("nombre", marca + " Desde panel"), ("codigo", ""),
                ("usuario", marca.ToLowerInvariant() + "-dueno"), ("admin_nombre", "Dueño"), ("clave", Clave), ("email", ""), ("ruc", ""));
            var nueva = await EmpresaAsync(marca.ToLowerInvariant() + "-desde-panel");
            Assert.NotNull(nueva);
            await AislamientoTests.EliminarEmpresaAsync(_app, nueva.id_empresa);
        }
    }
}
