using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    /// <summary>
    /// Ambiente del local (Salon, Terraza, 2do piso...). Cada zona tiene su propio plano.
    /// </summary>
    [Table("Zona")]
    public class MDZona
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_zona { get; set; }
        [Display(Name = "Nombre")]
        [Required, StringLength(50)]
        public string zona_desc { get; set; }
        [Display(Name = "Orden")]
        public int zona_orden { get; set; }
        [Display(Name = "Activo")]
        public bool zona_act { get; set; } = true;
    }
}
