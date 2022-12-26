using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Menu")]
    public class MDMenu
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_menu { get; set; }
        public string menu_desc { get; set; }
        public decimal menu_precio { get; set; }
        public string? menu_observ { get; set; }
        public DateTime menu_fecha { get; set; }
    }
}
