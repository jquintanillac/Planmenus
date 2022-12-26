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


    }
}
