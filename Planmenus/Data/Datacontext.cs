using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data.Entities;

namespace Planmenus.Data
{
    public class Datacontext : DbContext, IDataProtectionKeyContext
    {
        private readonly EmpresaActual _empresa;

        public Datacontext(DbContextOptions<Datacontext> options, EmpresaActual empresa) : base(options)
        {
            _empresa = empresa;
        }

        /// <summary>Empresa usada por los filtros globales (EF la evalua en cada consulta)</summary>
        public int? EmpresaId => _empresa?.Id;

        public virtual DbSet<MDEmpresa> Empresas { get; set; }
        public virtual DbSet<MDCaja> Cajas { get; set; }
        public virtual DbSet<MDCaja_Detalle> Caja_Detalles { get; set; }
        public virtual DbSet<MDMenu> Menus  { get; set; }
        public virtual DbSet<MDUsuarios> Usuarios { get; set; }
        public virtual DbSet<MDTipo_pago> Tipo_Pagos { get; set; }
        public virtual DbSet<MDDelivery> Deliveries { get; set; }
        public virtual DbSet<MDEventos> Eventos { get; set; }
        public virtual DbSet<MDMesas> Mesas { get; set; }
        public virtual DbSet<MDEgresos> Egresos { get; set; }
        public virtual DbSet<MDGenerales> Generales { get; set; }
        public virtual DbSet<MDIngresos> Ingresos { get; set; }
        public virtual DbSet<MDInicial> Inicial { get; set; }
        public virtual DbSet<MDMenuprin> Menuprins { get; set; }
        public virtual DbSet<MDSubmenu> Submenus { get; set; }
        public virtual DbSet<MDRol> Rols { get; set; }
        public virtual DbSet<MDRol_Usuario> Rol_Usuarios { get; set; }
        public virtual DbSet<MDRol_Permisos> Rol_Permisos { get; set; }
        public virtual DbSet<MDTurno_Caja> Turnos { get; set; }
        public virtual DbSet<MDZona> Zonas { get; set; }
        public virtual DbSet<MDAsignacion_Mozo> Asignaciones { get; set; }
        public virtual DbSet<MDReserva> Reservas { get; set; }
        public virtual DbSet<MDReserva_Mesa> Reserva_Mesas { get; set; }
        public virtual DbSet<MDCorreo_Log> Correos { get; set; }
        public virtual DbSet<MDTicketera> Ticketeras { get; set; }
        public virtual DbSet<MDImpresion_Cola> Impresiones { get; set; }
        public virtual DbSet<MDFacturacion> Facturacion { get; set; }
        public virtual DbSet<MDComprobante> Comprobantes { get; set; }
        public virtual DbSet<MDComprobante_Detalle> Comprobante_Detalles { get; set; }
        public virtual DbSet<MDSerie> Series { get; set; }
        /// <summary>Llaves de ASP.NET Data Protection (cookies de sesion y datos cifrados)</summary>
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Montos en soles con 2 decimales (coincide con las columnas existentes)
            modelBuilder.Entity<MDCaja>().Property(x => x.caja_monto).HasPrecision(18, 2);
            modelBuilder.Entity<MDCaja_Detalle>().Property(x => x.det_precio).HasPrecision(18, 2);
            modelBuilder.Entity<MDCaja_Detalle>().Property(x => x.det_subtotal).HasPrecision(18, 2);
            modelBuilder.Entity<MDMenu>().Property(x => x.menu_precio).HasPrecision(18, 2);
            modelBuilder.Entity<MDEgresos>().Property(x => x.egre_costo).HasPrecision(18, 2);
            modelBuilder.Entity<MDIngresos>().Property(x => x.ingre_monto).HasPrecision(18, 2);
            modelBuilder.Entity<MDInicial>().Property(x => x.ini_monto).HasPrecision(18, 2);
            modelBuilder.Entity<MDTurno_Caja>().Property(x => x.turno_monto_inicial).HasPrecision(18, 2);
            modelBuilder.Entity<MDTurno_Caja>().Property(x => x.turno_esperado).HasPrecision(18, 2);
            modelBuilder.Entity<MDTurno_Caja>().Property(x => x.turno_contado).HasPrecision(18, 2);
            modelBuilder.Entity<MDTurno_Caja>().Property(x => x.turno_diferencia).HasPrecision(18, 2);

            // Relaciones: una venta no puede apuntar a catalogos inexistentes
            modelBuilder.Entity<MDCaja>().HasOne<MDTipo_pago>().WithMany().HasForeignKey(x => x.id_tipago).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MDCaja>().HasOne<MDDelivery>().WithMany().HasForeignKey(x => x.id_deliv).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MDCaja>().HasOne<MDMesas>().WithMany().HasForeignKey(x => x.id_mesa).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MDCaja>().HasOne<MDMenu>().WithMany().HasForeignKey(x => x.id_menu).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MDCaja_Detalle>().HasOne<MDCaja>().WithMany().HasForeignKey(x => x.id_caja).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MDCaja_Detalle>().HasOne<MDMenu>().WithMany().HasForeignKey(x => x.id_menu).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MDEventos>().HasOne<MDMenu>().WithMany().HasForeignKey(x => x.id_menu).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MDSubmenu>().HasOne<MDMenuprin>().WithMany().HasForeignKey(x => x.id_menprin).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MDRol_Permisos>().HasOne<MDRol>().WithMany().HasForeignKey(x => x.id_rol).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MDRol_Permisos>().HasOne<MDMenuprin>().WithMany().HasForeignKey(x => x.id_menprin).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MDRol_Permisos>().HasOne<MDSubmenu>().WithMany().HasForeignKey(x => x.id_submenu).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MDRol_Usuario>().HasOne<MDUsuarios>().WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MDRol_Usuario>().HasOne<MDRol>().WithMany().HasForeignKey(x => x.id_rol).OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MDUsuarios>().HasIndex(x => x.Usua_user).IsUnique();
            modelBuilder.Entity<MDCaja>().HasIndex(x => x.caja_fecha);
            modelBuilder.Entity<MDCaja>().HasIndex(x => new { x.caja_pagado, x.caja_act });

            modelBuilder.Entity<MDMesas>().HasOne<MDZona>().WithMany().HasForeignKey(x => x.id_zona).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<MDAsignacion_Mozo>().HasOne<MDMesas>().WithMany().HasForeignKey(x => x.id_mesa).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MDAsignacion_Mozo>().HasOne<MDUsuarios>().WithMany().HasForeignKey(x => x.IdUsuario).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MDAsignacion_Mozo>().HasIndex(x => new { x.asig_fecha, x.id_mesa }).IsUnique();
            modelBuilder.Entity<MDReserva_Mesa>().HasOne<MDReserva>().WithMany().HasForeignKey(x => x.id_reserva).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MDReserva_Mesa>().HasOne<MDMesas>().WithMany().HasForeignKey(x => x.id_mesa).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<MDReserva_Mesa>().HasIndex(x => new { x.id_reserva, x.id_mesa }).IsUnique();
            modelBuilder.Entity<MDReserva>().HasIndex(x => new { x.res_inicio, x.res_estado });
            modelBuilder.Entity<MDCorreo_Log>().HasIndex(x => x.cor_fecha);

            // ---- Multiempresa ----
            modelBuilder.Entity<MDEmpresa>().HasIndex(x => x.emp_codigo).IsUnique();
            modelBuilder.Entity<MDSerie>().HasKey(x => new { x.id_empresa, x.serie_clave });
            modelBuilder.Entity<MDComprobante>().HasIndex(x => new { x.id_empresa, x.comp_tipo, x.comp_serie, x.comp_numero }).IsUnique();
            foreach (var tipo in modelBuilder.Model.GetEntityTypes().Where(t => typeof(IEmpresa).IsAssignableFrom(t.ClrType)).Select(t => t.ClrType).ToList())
            {
                modelBuilder.Entity(tipo).HasOne(typeof(MDEmpresa)).WithMany().HasForeignKey(nameof(IEmpresa.id_empresa)).OnDelete(DeleteBehavior.Restrict);
                modelBuilder.Entity(tipo).HasIndex(nameof(IEmpresa.id_empresa));
                // Va en el WHERE de cada UPDATE/DELETE: un registro de otra empresa nunca se modifica
                modelBuilder.Entity(tipo).Property(nameof(IEmpresa.id_empresa)).IsConcurrencyToken();
                // Cada empresa solo ve sus datos; sin empresa establecida no se ve nada
                var p = System.Linq.Expressions.Expression.Parameter(tipo, "e");
                var filtro = System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.Equal(
                        System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression.Property(p, nameof(IEmpresa.id_empresa)), typeof(int?)),
                        System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression.Constant(this), nameof(EmpresaId))),
                    p);
                modelBuilder.Entity(tipo).HasQueryFilter(filtro);
            }
            modelBuilder.Entity<MDImpresion_Cola>().HasIndex(x => new { x.imp_estado, x.imp_creado });

            modelBuilder.Entity<MDComprobante>().HasIndex(x => x.id_caja);
            modelBuilder.Entity<MDComprobante>().HasIndex(x => new { x.comp_estado, x.comp_fecha });
            modelBuilder.Entity<MDComprobante>().HasOne<MDCaja>().WithMany().HasForeignKey(x => x.id_caja).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<MDComprobante>().HasOne<MDComprobante>().WithMany().HasForeignKey(x => x.ref_id_comp).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<MDComprobante_Detalle>().HasOne<MDComprobante>().WithMany().HasForeignKey(x => x.id_comp).OnDelete(DeleteBehavior.Cascade);
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            AsignarEmpresa();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            AsignarEmpresa();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        /// <summary>
        /// Los registros nuevos toman la empresa actual; no se permite mover un registro a otra empresa.
        /// </summary>
        private void AsignarEmpresa()
        {
            foreach (var e in ChangeTracker.Entries<IEmpresa>())
            {
                if (e.State == EntityState.Added && e.Entity.id_empresa == 0)
                {
                    e.Entity.id_empresa = EmpresaId ?? throw new InvalidOperationException(
                        $"No se puede guardar {e.Entity.GetType().Name}: no hay una empresa establecida.");
                }
                else if (e.State is EntityState.Modified or EntityState.Deleted && EmpresaId != null)
                {
                    var prop = e.Property(x => x.id_empresa);
                    if (e.Entity.id_empresa == 0)
                    {
                        // Objeto armado desde un formulario (Update sin cargarlo): pertenece a la empresa actual
                        prop.CurrentValue = EmpresaId.Value;
                        prop.OriginalValue = EmpresaId.Value;
                        prop.IsModified = false;
                    }
                    else if (e.Entity.id_empresa != EmpresaId || (int)prop.OriginalValue != EmpresaId)
                    {
                        throw new InvalidOperationException("No se puede modificar un registro de otra empresa.");
                    }
                }
            }
        }
    }
}
