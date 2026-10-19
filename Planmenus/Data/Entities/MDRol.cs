using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Rol")]
    public class MDRol : IEmpresa
    {
        public int id_empresa { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_rol { get; set; }
        [Display(Name = "Descripcion")]
        public string rol_desc { get; set; }
        [Display(Name = "Activo")]
        public bool rol_act { get; set; }

    }
}
