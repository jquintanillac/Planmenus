using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Caja_Detalle")]
    public class MDCaja_Detalle : IEmpresa
    {
        public int id_empresa { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_detalle { get; set; }
        public int id_caja { get; set; }
        [Display(Name = "Menu")]
        public int id_menu { get; set; }
        [Display(Name = "Cantidad")]
        public int det_cant { get; set; }
        [Display(Name = "Precio")]
        public decimal det_precio { get; set; }
        [Display(Name = "Subtotal")]
        public decimal det_subtotal { get; set; }
    }
}
