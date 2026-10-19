using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.DataSql;
using Planmenus.Services;
using Planmenus.Services.Facturacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddScoped<EmpresaActual>();
builder.Services.AddDbContext<Datacontext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<SP_Menus>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<PlanoService>();
builder.Services.AddScoped<EmpresaService>();
builder.Services.AddScoped<SesionService>();
builder.Services.AddScoped<PlanService>();
builder.Services.Configure<SuscripcionOptions>(builder.Configuration.GetSection("Suscripcion"));
builder.Services.AddScoped<VentaService>();
builder.Services.AddScoped<TurnoService>();
builder.Services.AddScoped<CorreoService>();
builder.Services.AddScoped<ImpresionService>();
builder.Services.AddScoped<ReservaService>();
builder.Services.Configure<CorreoOptions>(builder.Configuration.GetSection("Correo"));
builder.Services.AddHostedService<RecordatorioWorker>();
builder.Services.AddSingleton<UblService>();
builder.Services.AddHttpClient<SunatClient>();
builder.Services.AddScoped<FacturacionService>();
builder.Services.AddHostedService<FacturacionWorker>();
builder.Services.AddSingleton<PdfService>();
builder.Services.AddSingleton<EscPosService>();

// QuestPDF es gratuito con licencia Community para empresas con ingresos anuales menores a 1 millon USD
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
builder.Services.AddAuthentication(options =>
{
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
}).AddCookie(options =>
{
    options.LoginPath = "/Home/Index";
    options.AccessDeniedPath = "/Home/AccesoDenegado";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddAuthorization(o => o.AddPolicy(Planmenus.Controllers.PlataformaController.PoliticaSuperadmin,
    p => p.RequireClaim(Planmenus.Helpers.SessionHelper.ClaimSuperadmin, "S")));
builder.Services.AddSession(o =>
{
    o.IdleTimeout = TimeSpan.FromSeconds(1800);
});
builder.Services.AddControllersWithViews(o =>
        {
            // Toda la aplicacion requiere sesion iniciada salvo lo marcado con [AllowAnonymous]
            o.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
        })
        .AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
            o.JsonSerializerOptions.PropertyNamingPolicy = null;
        });


// Llaves de proteccion en la base: las sesiones y los datos cifrados sobreviven a reinicios del hosting
builder.Services.AddDataProtection().PersistKeysToDbContext<Datacontext>().SetApplicationName("Planmenus");

// Registro publico de restaurantes: pocos intentos por IP para frenar abusos
builder.Services.AddMemoryCache();
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(Planmenus.Controllers.RegistroController.PoliticaRegistro, ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "", _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(15) }));
    o.AddPolicy(Planmenus.Controllers.RegistroController.PoliticaRegistro + "-consulta", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "", _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy(Planmenus.Controllers.RecuperarController.Politica, ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "", _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(15) }));
});

var app = builder.Build();

await DatosIniciales.CrearAdministradorAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
// Empresa de la peticion: la del usuario conectado (claim "empresa")
app.Use(async (ctx, next) =>
{
    if (ctx.User.Identity?.IsAuthenticated == true)
    {
        if (int.TryParse(ctx.User.FindFirst(Planmenus.Helpers.SessionHelper.ClaimEmpresa)?.Value, out var idEmpresa))
        {
            ctx.RequestServices.GetRequiredService<EmpresaActual>().Establecer(idEmpresa);

            // Empresa suspendida desde la plataforma: se cierran tambien las sesiones abiertas (cache de 1 minuto)
            var estado = await ctx.RequestServices.GetRequiredService<IMemoryCache>().GetOrCreateAsync(EstadoEmpresa.Clave(idEmpresa), async e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1);
                return await ctx.RequestServices.GetRequiredService<Datacontext>().Empresas.AsNoTracking()
                    .Where(x => x.id_empresa == idEmpresa)
                    .Select(x => new EstadoEmpresa(x.emp_activa, x.emp_vence)).FirstOrDefaultAsync();
            });
            if (estado?.Activa != true)
            {
                await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                ctx.Response.Redirect("/Home/Index");
                return;
            }
            ctx.RequestServices.GetRequiredService<EmpresaActual>().Vence = estado.Vence;

            // Suscripcion vencida (pasada la gracia): solo puede ver Mi suscripcion para renovarla
            var gracia = ctx.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<SuscripcionOptions>>().Value.DiasGracia;
            var ruta = ctx.Request.Path;
            if (PlanService.Bloqueada(estado.Vence, gracia)
                && ctx.User.FindFirst(Planmenus.Helpers.SessionHelper.ClaimSuperadmin) == null
                && !ruta.StartsWithSegments("/Suscripcion") && !ruta.StartsWithSegments("/Home/Logout"))
            {
                ctx.Response.Redirect("/Suscripcion");
                return;
            }
        }
        else
        {
            // Sesion anterior a la multiempresa: se pide ingresar de nuevo
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            ctx.Response.Redirect("/Home/Index");
            return;
        }
    }
    await next();
});
app.UseAuthorization();
app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();

// Permite que las pruebas de integracion (Planmenus.Tests) levanten la aplicacion
public partial class Program { }

/// <summary>Estado de la empresa que el middleware guarda en cache por un minuto</summary>
public record EstadoEmpresa(bool Activa, DateTime? Vence)
{
    public static string Clave(int idEmpresa) => "empresa-estado-" + idEmpresa;
}
