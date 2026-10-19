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

            // Se revisa en todas las empresas: solo se crea el administrador si el sistema esta vacio
            if (await context.Usuarios.IgnoreQueryFilters().AnyAsync())
            {
                return;
            }
            var empresa = await context.Empresas.OrderBy(e => e.id_empresa).FirstOrDefaultAsync();
            if (empresa == null)
            {
                logger.LogWarning("No hay empresas registradas; aplique las migraciones antes de iniciar.");
                return;
            }
            scope.ServiceProvider.GetRequiredService<EmpresaActual>().Establecer(empresa.id_empresa);

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
                Usua_act = true,
                // El primer usuario del sistema administra la plataforma
                Usua_superadmin = true
            };
            context.Usuarios.Add(admin);
            await context.SaveChangesAsync();

            var rol = await context.Rols.Where(r => r.rol_desc == "Administrador").Select(r => (int?)r.id_rol).FirstOrDefaultAsync() ?? RolAdministrador;
            context.Rol_Usuarios.Add(new MDRol_Usuario { IdUsuario = admin.IdUsuario, id_rol = rol });
            await context.SaveChangesAsync();

            logger.LogInformation("Usuario administrador '{Usuario}' creado.", usuario);
        }
    }
}
