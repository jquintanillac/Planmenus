using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Submenu")]
    public class MDSubmenu
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_submenu { get; set; }
        public int id_menprin { get; set; }
        [Display(Name = "Descripcion")]
        public string submenu_desc { get; set; }
        [Display(Name = "Nro. Orden")]
        public string submenu_nro { get; set; }
    }
}
