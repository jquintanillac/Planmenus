using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Mesas")]
    public class MDMesas
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_mesa { get; set; }
        [Display(Name = "Descripcion")]
        public string mesa_desc { get; set; }
        [Display(Name = "Nro. de mesa")]
        public int mesa_nro { get; set; }
        [Display(Name = "Activo")]
        public bool mesa_act { get; set; }
    }

}
