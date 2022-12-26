using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Egresos")]
    public class MDEgresos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_egre { get; set; }
        [Display(Name = "Descripcion")]
        public string egre_descr { get; set; }
        [Display(Name = "Costo")]
        public decimal egre_costo { get; set; }
        [Display(Name = "Fecha")]
        public DateTime egre_fecha { get; set; }
    }
}
