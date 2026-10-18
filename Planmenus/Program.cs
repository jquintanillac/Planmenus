using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.DataSql;
using Planmenus.Services;
using Planmenus.Services.Facturacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<Datacontext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<SP_Menus>();
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<PlanoService>();
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
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


app.Run();

// Permite que las pruebas de integracion (Planmenus.Tests) levanten la aplicacion
public partial class Program { }
