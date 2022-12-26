using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Ingresos")]
    public class MDIngresos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_ingre { get; set; }
        [Display(Name = "Descripcion")]
        public string ingre_desc { get; set; }
        [Display(Name = "Monto")]
        public decimal ingre_monto { get; set; }
        [Display(Name = "Fecha")]
        public DateTime ingre_fecha { get; set; }
    }
}
