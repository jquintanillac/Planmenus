using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Rol_Usuario")]
    public class MDRol_Usuario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_roluser { get; set; }
        public int IdUsuario { get; set; }
        public int id_rol { get; set; }

    }
}
