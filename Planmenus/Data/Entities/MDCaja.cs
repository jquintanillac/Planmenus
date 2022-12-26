using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Caja")]
    public class MDCaja
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_caja { get; set; }
        [Display(Name ="Menu")]
        public int id_menu { get; set; }
        [Display(Name = "Tipo de pago")]
        public int id_tipago { get; set; }
        [Display(Name = "Delivery")]
        public int? id_deliv { get; set; }
        [Display(Name = "Mesa")]
        public int? id_mesa { get; set; }
        [Display(Name = "Observacion")]
        public string? caja_obs { get; set; }
        [Display(Name = "Monto")]
        public decimal caja_monto { get; set; }
        [Display(Name = "Fecha")]
        public DateTime caja_fecha { get; set; }
        [Display(Name = "Activo")]
        public bool caja_act { get; set; }
    }
}
