using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    /// <summary>
    /// Registro de cada correo enviado (o fallido), para auditar y no duplicar avisos.
    /// </summary>
    [Table("Correo_Log")]
    public class MDCorreo_Log
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_correo { get; set; }
        public DateTime cor_fecha { get; set; }
        [StringLength(30)]
        public string cor_tipo { get; set; }
        public int? id_reserva { get; set; }
        [StringLength(1000)]
        public string cor_para { get; set; }
        [StringLength(200)]
        public string cor_asunto { get; set; }
        public bool cor_ok { get; set; }
        [StringLength(1000)]
        public string cor_error { get; set; }
    }
}
