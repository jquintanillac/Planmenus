using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Rol_Permisos")]
    public class MDRol_Permisos
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_rolper { get; set; }
        public int id_rol { get; set; }
        public int id_menprin { get; set; }
        public int id_submenu { get; set; }
        public bool rolper_act{ get; set; }
    }
}
