using Microsoft.EntityFrameworkCore;
using Planmenus.Data.Entities;
using Planmenus.Helpers;

namespace Planmenus.Data
{
    /// <summary>
    /// Crea el usuario administrador cuando la base de datos no tiene usuarios.
    /// Los catalogos (menus, submenus, rol y permisos) los crea la migracion DatosIniciales.
    /// La contraseña se toma de la configuracion (Seed:AdminPassword), nunca del codigo.
    /// </summary>
    public static class DatosIniciales
    {
        private const int RolAdministrador = 1;

        public static async Task CrearAdministradorAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<Datacontext>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatosIniciales));

            if (await context.Usuarios.AnyAsync())
            {
                return;
            }

            var usuario = config["Seed:AdminUser"] ?? "admin";
            var password = config["Seed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("No hay usuarios y no se configuro Seed:AdminPassword; no se creo el administrador.");
                return;
            }

            var hash = HashHelper.Hash(password);
            var admin = new MDUsuarios
            {
                Usua_user = usuario,
                Usua_nombres = "Administrador",
                Usua_apellidos = "",
                Usua_email = config["Seed:AdminEmail"] ?? "",
                Usua_pass = hash.Password,
                Usua_Hash = hash.Salt,
                Usua_act = true
            };
            context.Usuarios.Add(admin);
            await context.SaveChangesAsync();

            context.Rol_Usuarios.Add(new MDRol_Usuario { IdUsuario = admin.IdUsuario, id_rol = RolAdministrador });
            await context.SaveChangesAsync();

            logger.LogInformation("Usuario administrador '{Usuario}' creado.", usuario);
        }
    }
}
