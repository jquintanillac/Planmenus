using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Menuprin")]
    public class MDMenuprin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_menprin { get; set; }
        [Display(Name = "Descripcion")]
        public string menuprin_desc { get; set; }
        [Display(Name = "Activo")]
        public bool menuprin_act { get; set; }
    }
}
