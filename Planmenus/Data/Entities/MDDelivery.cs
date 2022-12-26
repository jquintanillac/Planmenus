using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Delivery")]
    public class MDDelivery
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_deliv { get; set; }
        [Display(Name = "Codigo")]
        public string deliv_codig { get; set; }
        [Display(Name = "Nombre Completo")]
        public string deliv_nombcom { get; set; }
        [Display(Name = "Descripcion")]        
        public string deliv_desc { get; set; }
        [Display(Name = "Activo")]
        public bool deliv_act { get; set; }
    }
}
