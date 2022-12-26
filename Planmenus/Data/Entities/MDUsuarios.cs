using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Usuarios")]
    public class MDUsuarios
    {
        [Key]
        [Display(Name = "ID")]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdUsuario { get; set; }
        [Display(Name = "Usuario")]
        public string Usua_user { get; set; }
        [Display(Name = "Nombre")]
        public string Usua_nombres { get; set; }
        [Display(Name = "Apellido")]
        public string Usua_apellidos { get; set; }
        [Display(Name = "email")]
        public string Usua_email { get; set; }
        [Display(Name = "Contraseña")]
        public string Usua_pass { get; set; }
        [Display(Name = "Contraseña Hash")]
        public string Usua_Hash { get; set; }

        [Display(Name = "Activo")]
        public bool Usua_act { get; set; }
    }
}
