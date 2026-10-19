using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Planmenus.Data;
using Planmenus.Services;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Planmenus.Tests
{
    /// <summary>
    /// Levanta la aplicacion real (con la base configurada en user-secrets) y un usuario de pruebas
    /// con todos los permisos. Los procesos en segundo plano (recordatorios, reenvio a SUNAT) se desactivan.
    /// </summary>
    public class AppFactory : WebApplicationFactory<Program>
    {
        public const string Esquema = "Pruebas";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(services =>
            {
                foreach (var d in services.Where(d => d.ServiceType == typeof(IHostedService)).ToList())
                {
                    services.Remove(d);
                }
                // Correo y SUNAT simulados: nada sale a internet
                services.AddSingleton<Buzon>();
                foreach (var d in services.Where(d => d.ServiceType == typeof(CorreoService)).ToList())
                {
                    services.Remove(d);
                }
                services.AddScoped<CorreoService, CorreoFalso>();
                services.PostConfigure<CorreoOptions>(o => { o.Host = "smtp.pruebas.local"; o.Remitente = "pruebas@planmenus.pe"; o.NombreRemitente = "Planmenus"; });
                services.AddHttpClient<Planmenus.Services.Facturacion.SunatClient>().ConfigurePrimaryHttpMessageHandler(() => new SunatSinRed());

                // Mercado Pago simulado: las pruebas nunca llaman a la API real
                foreach (var d in services.Where(d => d.ServiceType == typeof(Planmenus.Services.Pagos.IMercadoPago)).ToList())
                {
                    services.Remove(d);
                }
                services.AddSingleton<MercadoPagoFalso>();
                services.AddSingleton<Planmenus.Services.Pagos.IMercadoPago>(sp => sp.GetRequiredService<MercadoPagoFalso>());
                services.PostConfigure<Planmenus.Services.Pagos.MercadoPagoOptions>(o => o.WebhookSecret = MercadoPagoFalso.ClaveWebhook);
                services.AddAuthentication(Esquema).AddScheme<AuthenticationSchemeOptions, UsuarioPruebas>(Esquema, _ => { });
                services.PostConfigure<AuthenticationOptions>(o =>
                {
                    o.DefaultScheme = Esquema;
                    o.DefaultAuthenticateScheme = Esquema;
                    o.DefaultChallengeScheme = Esquema;
                });
            });
        }

        public Cliente Cliente() => new(CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true }), this);

        /// <summary>Acceso directo a la base dentro de una empresa (1 por defecto)</summary>
        public async Task Con(Func<Datacontext, Task> accion, int empresa = 1)
        {
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<EmpresaActual>().Establecer(empresa);
            await accion(scope.ServiceProvider.GetRequiredService<Datacontext>());
        }

        public async Task<T> Con<T>(Func<Datacontext, Task<T>> accion, int empresa = 1)
        {
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<EmpresaActual>().Establecer(empresa);
            return await accion(scope.ServiceProvider.GetRequiredService<Datacontext>());
        }

        /// <summary>Cliente que trabaja como la empresa indicada</summary>
        public Cliente ClienteDe(int empresa)
        {
            var c = Cliente();
            c.Http.DefaultRequestHeaders.Add(UsuarioPruebas.CabeceraEmpresa, empresa.ToString());
            return c;
        }

        /// <summary>Visitante sin sesion iniciada</summary>
        public Cliente Anonimo()
        {
            var c = Cliente();
            c.Http.DefaultRequestHeaders.Add(UsuarioPruebas.CabeceraAnonimo, "1");
            return c;
        }

        /// <summary>Administrador de la empresa 1 que ademas administra la plataforma</summary>
        public Cliente Superadmin()
        {
            var c = Cliente();
            c.Http.DefaultRequestHeaders.Add(UsuarioPruebas.CabeceraSuperadmin, "1");
            return c;
        }
    }

    /// <summary>Usuario "admin" con todos los permisos del menu</summary>
    public class UsuarioPruebas : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string CabeceraEmpresa = "X-Prueba-Empresa";

        public UsuarioPruebas(IOptionsMonitor<AuthenticationSchemeOptions> o, ILoggerFactory l, UrlEncoder e) : base(o, l, e) { }

        public const string CabeceraAnonimo = "X-Prueba-Anonimo";
        public const string CabeceraSuperadmin = "X-Prueba-Superadmin";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Visitante sin sesion (registro publico, login)
            if (Request.Headers.ContainsKey(CabeceraAnonimo))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, "admin"),
                new(ClaimTypes.Name, "Pruebas automatizadas"),
                new(ClaimTypes.Role, "Administrador"),
                // Empresa 1 por defecto; las pruebas de aislamiento usan la cabecera X-Prueba-Empresa
                new(Planmenus.Helpers.SessionHelper.ClaimEmpresa, Request.Headers.TryGetValue(CabeceraEmpresa, out var e) ? e.ToString() : "1")
            };
            for (int m = 1; m <= 3; m++) claims.Add(new Claim("menu" + m, "S"));
            for (int m = 1; m <= 3; m++)
                for (int s = 1; s <= 15; s++)
                    claims.Add(new Claim($"submenu{m}_{s}", "S"));
            if (Request.Headers.ContainsKey(CabeceraSuperadmin)) claims.Add(new Claim(Planmenus.Helpers.SessionHelper.ClaimSuperadmin, "S"));
            var identidad = new ClaimsIdentity(claims, AppFactory.Esquema, ClaimTypes.Name, ClaimTypes.Role);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidad), AppFactory.Esquema)));
        }
    }

    /// <summary>
    /// Cliente HTTP que conserva cookies y obtiene el token antifalsificacion de cada formulario.
    /// </summary>
    public class Cliente
    {
        private static readonly Regex Token = new("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"", RegexOptions.Compiled);
        public HttpClient Http { get; }
        public AppFactory App { get; }

        public Cliente(HttpClient http, AppFactory app)
        {
            Http = http;
            App = app;
        }

        public async Task<string> HtmlAsync(string url)
        {
            var r = await Http.GetAsync(url);
            Assert.True(r.StatusCode == HttpStatusCode.OK, $"GET {url} devolvió {(int)r.StatusCode}");
            return await r.Content.ReadAsStringAsync();
        }

        public async Task<string> TokenAsync(string desde)
        {
            var m = Token.Match(await HtmlAsync(desde));
            Assert.True(m.Success, $"No se encontró el token del formulario en {desde}");
            return WebUtility.HtmlDecode(m.Groups[1].Value);
        }

        /// <summary>Envia un formulario (tomando el token de la pagina 'desde') y devuelve la respuesta</summary>
        public async Task<HttpResponseMessage> PostAsync(string desde, string url, params (string, object)[] campos)
        {
            var datos = campos.Select(c => new KeyValuePair<string, string>(c.Item1, Convert.ToString(c.Item2, System.Globalization.CultureInfo.InvariantCulture))).ToList();
            datos.Add(new("__RequestVerificationToken", await TokenAsync(desde)));
            return await Http.PostAsync(url, new FormUrlEncodedContent(datos));
        }

        /// <summary>Envia el formulario y exige una redireccion (operacion exitosa)</summary>
        public async Task<string> PostOkAsync(string desde, string url, params (string, object)[] campos)
        {
            var r = await PostAsync(desde, url, campos);
            var cuerpo = await r.Content.ReadAsStringAsync();
            Assert.True(r.StatusCode == HttpStatusCode.Redirect, $"POST {url} devolvió {(int)r.StatusCode}: {Recortar(cuerpo)}");
            return r.Headers.Location?.ToString();
        }

        public async Task<HttpResponseMessage> PostJsonAsync(string desde, string url, object cuerpo)
        {
            var token = await TokenAsync(desde);
            var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(cuerpo) };
            req.Headers.Add("RequestVerificationToken", token);
            return await Http.SendAsync(req);
        }

        private static string Recortar(string s) => s.Length > 400 ? s[..400] : s;
    }

    [CollectionDefinition("App")]
    public class ColeccionApp : ICollectionFixture<AppFactory> { }

    public static class Marca
    {
        /// <summary>Prefijo unico para los registros que crean las pruebas</summary>
        public static string Nueva() => "TEST-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
    }
}
