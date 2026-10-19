using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Inicial")]
    public class MDInicial : IEmpresa
    {
        public int id_empresa { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_ini { get; set; }
        [Display(Name = "Descripcion")]
        public string ini_desc { get; set; }
        [Display(Name = "Monto")]
        public decimal ini_monto { get; set; }
        [Display(Name = "Fecha")]
        public DateTime ini_fecha { get; set; }
    }
}
