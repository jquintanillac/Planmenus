using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Caja")]
    public class MDCaja : IEmpresa
    {
        public int id_empresa { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_caja { get; set; }
        [Display(Name ="Menu")]
        public int? id_menu { get; set; }
        [Display(Name = "Tipo de pago")]
        public int? id_tipago { get; set; }
        [Display(Name = "Delivery")]
        public int? id_deliv { get; set; }
        [Display(Name = "Mesa")]
        public int? id_mesa { get; set; }
        [Display(Name = "Observacion")]
        public string caja_obs { get; set; }
        [Display(Name = "Monto")]
        public decimal caja_monto { get; set; }
        [Display(Name = "Fecha")]
        public DateTime caja_fecha { get; set; }
        [Display(Name = "Activo")]
        public bool caja_act { get; set; }
        [Display(Name = "Usuario")]
        [StringLength(50)]
        public string caja_usuario { get; set; }
        /// <summary>false = pedido abierto (en mesa o por cobrar)</summary>
        [Display(Name = "Pagado")]
        public bool caja_pagado { get; set; } = true;
        [Display(Name = "Fecha de cobro")]
        public DateTime? caja_fecha_pago { get; set; }
        /// <summary>Estado en cocina, ver <see cref="EstadoCocina"/></summary>
        [Display(Name = "Cocina")]
        public byte caja_cocina { get; set; }
        /// <summary>Usuario del mozo que atiende el pedido</summary>
        [Display(Name = "Mozo")]
        [StringLength(50)]
        public string caja_mozo { get; set; }
    }
}

namespace Planmenus.Data.Entities
{
    public static class EstadoCocina
    {
        public const byte EnCocina = 0;
        public const byte Listo = 1;
        public const byte Entregado = 2;
    }
}
