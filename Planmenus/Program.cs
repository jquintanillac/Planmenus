using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.DataSql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<Datacontext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<SP_Menus>();
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

Rotativa.AspNetCore.RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

app.Run();
