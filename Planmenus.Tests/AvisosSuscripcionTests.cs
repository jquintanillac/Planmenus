using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MimeKit;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Services;
using Planmenus.Services.Facturacion;
using Planmenus.Services.Pagos;

namespace Planmenus.Tests
{
    /// <summary>
    /// Bloqueo del login, comprobante y recibo de cada pago de suscripcion y avisos de vencimiento.
    /// Correo y SUNAT son simulados (CorreoFalso, SunatSinRed).
    /// </summary>
    [Collection("App")]
    public class AvisosSuscripcionTests
    {
        private const string Clave = "ClaveSegura123";
        private readonly AppFactory _app;
        private Buzon Buzon => _app.Services.GetRequiredService<Buzon>();

        public AvisosSuscripcionTests(AppFactory app)
        {
            _app = app;
        }

        private async Task<MDEmpresa> CrearAsync(string marca, string nombre = " Avisos", string ruc = null)
        {
            using var scope = _app.Services.CreateScope();
            var r = await scope.ServiceProvider.GetRequiredService<EmpresaService>().CrearAsync(new EmpresaService.Alta(
                marca + nombre, EmpresaService.Codigo(marca + nombre), ruc, EmpresaService.Codigo(marca + nombre) + "-admin", "Dueña", Clave, Correo(marca, nombre)));
            Assert.True(r.Ok, r.Error);
            return r.Empresa;
        }

        private static string Correo(string marca, string nombre = " Avisos") => EmpresaService.Codigo(marca + nombre) + "@ejemplo.pe";

        private static string RucValido(string base10) => Enumerable.Range(0, 10).Select(d => base10 + d).First(Documentos.RucValido);

        private async Task<T> EnScopeAsync<T>(Func<AvisosSuscripcionService, Task<T>> accion)
        {
            using var scope = _app.Services.CreateScope();
            return await accion(scope.ServiceProvider.GetRequiredService<AvisosSuscripcionService>());
        }

        private Task<string> LoginAsync(string usuario, string clave) => _app.Anonimo()
            .PostAsync("/Home/Index", "/Home/Login", ("Usua_user", usuario), ("Usua_pass", clave))
            .ContinueWith(t => t.Result.Headers.Location?.ToString());

        [Fact]
        public async Task Login_se_bloquea_tras_cinco_claves_equivocadas()
        {
            var marca = Marca.Nueva();
            var b = await CrearAsync(marca, " Login");
            var usuario = EmpresaService.Codigo(marca + " Login") + "-admin";
            try
            {
                for (int i = 0; i < HomeController_IntentosPermitidos; i++)
                {
                    Assert.Equal("/", await LoginAsync(usuario, "equivocada" + i));
                }
                // Bloqueado: ni con la clave correcta entra
                Assert.Equal("/", await LoginAsync(usuario.ToUpperInvariant(), Clave));

                // Pasado el bloqueo (se simula olvidando el contador) entra normal y el contador vuelve a cero
                _app.Services.GetRequiredService<IMemoryCache>().Remove("login-fallos-" + usuario);
                Assert.Equal("/Home/Inicio", await LoginAsync(usuario, Clave));
                Assert.Equal("/", await LoginAsync(usuario, "equivocada"));
                Assert.Equal("/Home/Inicio", await LoginAsync(usuario, Clave));
            }
            finally
            {
                _app.Services.GetRequiredService<IMemoryCache>().Remove("login-fallos-" + usuario);
                await AislamientoTests.EliminarEmpresaAsync(_app, b.id_empresa);
            }
        }

        private const int HomeController_IntentosPermitidos = Planmenus.Controllers.HomeController.IntentosPermitidos;

        [Fact]
        public async Task Cada_pago_emite_su_comprobante_y_envia_el_recibo_una_vez()
        {
            var marca = Marca.Nueva();
            var opciones = _app.Services.GetRequiredService<IOptions<SuscripcionOptions>>().Value;
            var emisoraOriginal = opciones.EmpresaEmisora;
            var emisora = await CrearAsync(marca, " Plataforma");
            var b = await CrearAsync(marca, " Cliente");
            try
            {
                // La plataforma de pruebas factura en el ambiente de pruebas con certificado autofirmado
                await _app.Con(async db =>
                {
                    using var scope = _app.Services.CreateScope();
                    scope.ServiceProvider.GetRequiredService<EmpresaActual>().Establecer(emisora.id_empresa);
                    var fe = new MDFacturacion
                    {
                        fe_activa = true, fe_ambiente = AmbienteFe.Pruebas, fe_ruc = RucValido("2060012345"),
                        fe_razon_social = "PLATAFORMA DE PRUEBAS SAC", fe_direccion = "AV. PRUEBA 123", fe_ubigeo = "150101"
                    };
                    scope.ServiceProvider.GetRequiredService<FacturacionService>().GenerarCertificadoPrueba(fe);
                    db.Facturacion.Add(fe);
                    await db.SaveChangesAsync();
                }, emisora.id_empresa);
                opciones.EmpresaEmisora = emisora.id_empresa;

                // Pago manual: el restaurante no tiene RUC → boleta
                var admin = _app.Superadmin();
                await admin.PostOkAsync("/Plataforma", "/Plataforma/Pago", ("id", b.id_empresa), ("plan", "estandar"), ("meses", 1), ("monto", ""), ("medio", "Yape"), ("referencia", "OP-1"));
                Assert.True(await EnScopeAsync(s => s.ProcesarPagosAsync()) >= 1);

                var pago = await _app.Con(db => db.Pagos_Suscripcion.AsNoTracking().SingleAsync(), b.id_empresa);
                Assert.True(pago.pago_procesado);
                Assert.StartsWith("B001-", pago.pago_comprobante);
                Assert.Null(pago.pago_nota);

                var boleta = await _app.Con(db => db.Comprobantes.AsNoTracking().SingleAsync(), emisora.id_empresa);
                Assert.Equal(89m, boleta.comp_total);
                Assert.Equal(TipoComprobante.Boleta, boleta.comp_tipo);
                Assert.Equal(EstadoComprobante.ErrorEnvio, boleta.comp_estado);  // sin red: queda para reenviar

                var recibo = Assert.Single(Buzon.Para(Correo(marca, " Cliente")));
                Assert.Contains("Recibo", recibo.Subject);
                Assert.Contains(pago.pago_comprobante, recibo.HtmlBody);
                Assert.Contains(recibo.Attachments, a => a.ContentDisposition.FileName == pago.pago_comprobante + ".xml");

                // Procesar otra vez no repite nada
                await EnScopeAsync(s => s.ProcesarPagosAsync());
                Assert.Single(Buzon.Para(Correo(marca, " Cliente")));
                Assert.Equal(1, await _app.Con(db => db.Comprobantes.CountAsync(), emisora.id_empresa));

                // Con RUC → factura a su razon social
                var ruc = RucValido("2061234567");
                await _app.Con(db => db.Empresas.Where(e => e.id_empresa == b.id_empresa).ExecuteUpdateAsync(s => s.SetProperty(e => e.emp_ruc, ruc)));
                await admin.PostOkAsync("/Plataforma", "/Plataforma/Pago", ("id", b.id_empresa), ("plan", "basico"), ("meses", 1), ("monto", ""), ("medio", "Transferencia"), ("referencia", ""));
                await EnScopeAsync(s => s.ProcesarPagosAsync());
                var factura = await _app.Con(db => db.Comprobantes.AsNoTracking().SingleAsync(c => c.comp_tipo == TipoComprobante.Factura), emisora.id_empresa);
                Assert.Equal(ruc, factura.cli_num_doc);
                Assert.Equal(2, Buzon.Para(Correo(marca, " Cliente")).Count);

                // El panel muestra el comprobante de cada pago
                Assert.Contains(pago.pago_comprobante, await admin.HtmlAsync("/Plataforma"));
            }
            finally
            {
                opciones.EmpresaEmisora = emisoraOriginal;
                await AislamientoTests.EliminarEmpresaAsync(_app, b.id_empresa);
                await AislamientoTests.EliminarEmpresaAsync(_app, emisora.id_empresa);
            }
        }

        [Fact]
        public async Task Sin_facturacion_activa_el_recibo_igual_se_envia()
        {
            var marca = Marca.Nueva();
            var opciones = _app.Services.GetRequiredService<IOptions<SuscripcionOptions>>().Value;
            var emisoraOriginal = opciones.EmpresaEmisora;
            var emisora = await CrearAsync(marca, " Sin FE");
            var b = await CrearAsync(marca, " Cliente");
            try
            {
                opciones.EmpresaEmisora = emisora.id_empresa;
                await _app.Superadmin().PostOkAsync("/Plataforma", "/Plataforma/Pago", ("id", b.id_empresa), ("plan", "basico"), ("meses", 1), ("monto", ""), ("medio", "Yape"), ("referencia", ""));
                await EnScopeAsync(s => s.ProcesarPagosAsync());

                var pago = await _app.Con(db => db.Pagos_Suscripcion.AsNoTracking().SingleAsync(), b.id_empresa);
                Assert.True(pago.pago_procesado);
                Assert.Null(pago.pago_comprobante);
                Assert.Contains("Sin comprobante", pago.pago_nota);
                Assert.Single(Buzon.Para(Correo(marca, " Cliente")));
            }
            finally
            {
                opciones.EmpresaEmisora = emisoraOriginal;
                await AislamientoTests.EliminarEmpresaAsync(_app, b.id_empresa);
                await AislamientoTests.EliminarEmpresaAsync(_app, emisora.id_empresa);
            }
        }

        [Fact]
        public async Task Avisos_de_vencimiento_se_envian_una_vez_por_periodo()
        {
            var marca = Marca.Nueva();
            var b = await CrearAsync(marca);
            var correo = Correo(marca);
            Task VenceAsync(DateTime vence) => _app.Con(db => db.Empresas.Where(e => e.id_empresa == b.id_empresa).ExecuteUpdateAsync(s => s.SetProperty(e => e.emp_vence, vence)));
            List<MimeMessage> Recibidos() => Buzon.Para(correo);
            try
            {
                // Recien creada (14 dias de prueba): todavia no toca avisar
                await EnScopeAsync(s => s.AvisarVencimientosAsync());
                Assert.Empty(Recibidos());

                // A 3 dias: aviso de fin de la prueba gratis, una sola vez
                await VenceAsync(Fechas.HoyPeru().AddDays(3));
                await EnScopeAsync(s => s.AvisarVencimientosAsync());
                await EnScopeAsync(s => s.AvisarVencimientosAsync());
                var aviso = Assert.Single(Recibidos());
                Assert.Contains("prueba gratis termina en 3 días", aviso.Subject);

                // Vencida ayer: aviso de vencimiento con los dias de gracia que quedan
                await VenceAsync(Fechas.HoyPeru().AddDays(-1));
                await EnScopeAsync(s => s.AvisarVencimientosAsync());
                Assert.Equal(2, Recibidos().Count);
                Assert.Contains(Recibidos(), m => m.Subject.Contains("venció") && m.HtmlBody.Contains("2 días más"));

                // Pasada la gracia ya no se insiste
                await VenceAsync(Fechas.HoyPeru().AddDays(-10));
                await EnScopeAsync(s => s.AvisarVencimientosAsync());
                Assert.Equal(2, Recibidos().Count);

                // Nuevo periodo (otra fecha de vencimiento): vuelve a avisar
                await VenceAsync(Fechas.HoyPeru().AddDays(5));
                await EnScopeAsync(s => s.AvisarVencimientosAsync());
                Assert.Equal(3, Recibidos().Count);
            }
            finally
            {
                await AislamientoTests.EliminarEmpresaAsync(_app, b.id_empresa);
            }
        }
    }
}
