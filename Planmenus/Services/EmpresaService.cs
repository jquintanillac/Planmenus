using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Planmenus.Services
{
    /// <summary>
    /// Alta de empresas (multiempresa): crea la empresa con lo minimo para empezar a trabajar
    /// y permite ejecutar tareas "dentro" de cada empresa (procesos en segundo plano).
    /// </summary>
    public class EmpresaService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly SuscripcionOptions _suscripcion;

        public EmpresaService(IServiceScopeFactory scopes, Microsoft.Extensions.Options.IOptions<SuscripcionOptions> suscripcion)
        {
            _scopes = scopes;
            _suscripcion = suscripcion.Value;
        }

        public record Alta(string Nombre, string Codigo, string Ruc, string AdminUsuario, string AdminNombre, string AdminClave, string AdminEmail = null);
        public record Resultado(MDEmpresa Empresa, string Error)
        {
            public bool Ok => Error == null;
        }

        /// <summary>
        /// Crea la empresa con: rol Administrador (todos los permisos), su usuario administrador,
        /// tipos de pago basicos, una zona "Salón" y los datos generales del negocio.
        /// </summary>
        public async Task<Resultado> CrearAsync(Alta a)
        {
            var codigo = Codigo(string.IsNullOrWhiteSpace(a.Codigo) ? a.Nombre : a.Codigo);
            if (string.IsNullOrWhiteSpace(a.Nombre)) return new Resultado(null, "Ingrese el nombre de la empresa.");
            if (codigo.Length < 3) return new Resultado(null, "El código de la empresa debe tener al menos 3 letras o números.");
            if (string.IsNullOrWhiteSpace(a.AdminUsuario) || a.AdminUsuario.Length < 3) return new Resultado(null, "El usuario administrador debe tener al menos 3 caracteres.");
            if (string.IsNullOrWhiteSpace(a.AdminClave) || a.AdminClave.Length < 8) return new Resultado(null, "La contraseña debe tener al menos 8 caracteres.");

            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Datacontext>();
            var usuario = a.AdminUsuario.Trim().ToLowerInvariant();
            if (await db.Empresas.AnyAsync(e => e.emp_codigo == codigo)) return new Resultado(null, $"El código \"{codigo}\" ya está en uso.");
            if (await db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Usua_user == usuario)) return new Resultado(null, $"El usuario \"{usuario}\" ya existe.");

            await using var tx = await db.Database.BeginTransactionAsync();
            var empresa = new MDEmpresa { emp_nombre = a.Nombre.Trim(), emp_codigo = codigo, emp_ruc = a.Ruc?.Trim(), emp_activa = true, emp_creada = Fechas.AhoraPeru(),
                // Periodo de prueba: el plan basico gratis por unos dias
                emp_plan = _suscripcion.Todos[0].Codigo, emp_vence = Fechas.HoyPeru().AddDays(_suscripcion.DiasPrueba) };
            db.Empresas.Add(empresa);
            await db.SaveChangesAsync();

            // Todo lo que sigue pertenece a la nueva empresa
            scope.ServiceProvider.GetRequiredService<EmpresaActual>().Establecer(empresa.id_empresa);

            db.Generales.Add(new MDGenerales { gene_empre = empresa.emp_nombre, gene_ruc = empresa.emp_ruc, gene_recordatorio_horas = 2, gene_reserva_minutos = 120 });
            db.Tipo_Pagos.AddRange(
                new MDTipo_pago { tipago_desc = "Efectivo", tipago_act = true, tipago_efectivo = true },
                new MDTipo_pago { tipago_desc = "Yape", tipago_act = true },
                new MDTipo_pago { tipago_desc = "Plin", tipago_act = true },
                new MDTipo_pago { tipago_desc = "Tarjeta", tipago_act = true });
            db.Zonas.Add(new MDZona { zona_desc = "Salón", zona_orden = 1, zona_act = true });

            var rol = new MDRol { rol_desc = "Administrador", rol_act = true };
            db.Rols.Add(rol);
            await db.SaveChangesAsync();
            var submenus = await db.Submenus.AsNoTracking().ToListAsync();
            db.Rol_Permisos.AddRange(submenus.Select(s => new MDRol_Permisos { id_rol = rol.id_rol, id_menprin = s.id_menprin, id_submenu = s.id_submenu, rolper_act = true }));

            var hash = HashHelper.Hash(a.AdminClave);
            var admin = new MDUsuarios
            {
                Usua_user = usuario, Usua_nombres = string.IsNullOrWhiteSpace(a.AdminNombre) ? "Administrador" : a.AdminNombre.Trim(),
                Usua_apellidos = "", Usua_email = a.AdminEmail?.Trim() ?? "", Usua_pass = hash.Password, Usua_Hash = hash.Salt, Usua_act = true
            };
            db.Usuarios.Add(admin);
            await db.SaveChangesAsync();
            db.Rol_Usuarios.Add(new MDRol_Usuario { IdUsuario = admin.IdUsuario, id_rol = rol.id_rol });
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return new Resultado(empresa, null);
        }

        /// <summary>Ejecuta una tarea por cada empresa activa, con su propia conexion y su empresa establecida</summary>
        public async Task PorCadaEmpresaAsync(Func<IServiceProvider, Task> tarea, CancellationToken ct = default)
        {
            List<int> ids;
            using (var scope = _scopes.CreateScope())
            {
                ids = await scope.ServiceProvider.GetRequiredService<Datacontext>().Empresas
                    .Where(e => e.emp_activa).OrderBy(e => e.id_empresa).Select(e => e.id_empresa).ToListAsync(ct);
            }
            foreach (var id in ids)
            {
                ct.ThrowIfCancellationRequested();
                using var scope = _scopes.CreateScope();
                scope.ServiceProvider.GetRequiredService<EmpresaActual>().Establecer(id);
                await tarea(scope.ServiceProvider);
            }
        }

        /// <summary>Ejecuta una tarea dentro de una empresa concreta (por ejemplo, desde el panel de la plataforma)</summary>
        public async Task<T> EnEmpresaAsync<T>(int idEmpresa, Func<IServiceProvider, Task<T>> tarea)
        {
            using var scope = _scopes.CreateScope();
            scope.ServiceProvider.GetRequiredService<EmpresaActual>().Establecer(idEmpresa);
            return await tarea(scope.ServiceProvider);
        }

        /// <summary>"Cevichería Doña Pepa" → "cevicheria-dona-pepa"</summary>
        public static string Codigo(string texto)
        {
            var normal = (texto ?? "").Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normal)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            var codigo = Regex.Replace(sb.ToString(), "[^a-z0-9]+", "-").Trim('-');
            return codigo.Length > 50 ? codigo[..50].Trim('-') : codigo;
        }
    }
}
