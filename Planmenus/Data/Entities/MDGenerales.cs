using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Generales")]
    public class MDGenerales
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_gene { get; set; }
        [Display(Name = "Empresa")]
        public string gene_empre { get; set; }
        [Display(Name = "Ruc")]
        public string gene_ruc { get; set; }
        [Display(Name = "Direccion")]
        public string? gene_direc { get; set; }
        [Display(Name = "Telefono")]
        public string? gene_telef { get; set; }
        [Display(Name = "Direccion web")]
        public string? gene_url { get; set; }
        
    }
}
