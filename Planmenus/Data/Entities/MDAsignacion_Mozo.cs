using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    /// <summary>
    /// Mozo que atiende cada mesa en una fecha (planificacion del dia).
    /// </summary>
    [Table("Asignacion_Mozo")]
    public class MDAsignacion_Mozo : IEmpresa
    {
        public int id_empresa { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_asig { get; set; }
        [Column(TypeName = "date")]
        public DateTime asig_fecha { get; set; }
        public int id_mesa { get; set; }
        public int IdUsuario { get; set; }
    }
}
