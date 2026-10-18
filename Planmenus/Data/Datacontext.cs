using Microsoft.EntityFrameworkCore;
using Planmenus.Data.Entities;

namespace Planmenus.Data
{
    public class Datacontext : DbContext
    {
        public Datacontext(DbContextOptions<Datacontext> options) : base(options)
        {
        }
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
        }
    }
}
