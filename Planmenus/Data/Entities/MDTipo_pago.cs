using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Tipo_Pago")]
    public class MDTipo_pago
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_tipago { get; set; }
        [Display(Name = "Descripcion")]
        public string tipago_desc { get; set; }
        [Display(Name = "Activo")]
        public bool tipago_act { get; set; }
    }
}
