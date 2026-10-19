using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    /// <summary>
    /// Empresa cliente del sistema (multiempresa). Cada registro de negocio pertenece a una empresa.
    /// </summary>
    [Table("Empresa")]
    public class MDEmpresa
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_empresa { get; set; }
        [Required, StringLength(150)]
        public string emp_nombre { get; set; }
        /// <summary>Codigo corto y unico usado en direcciones publicas (carta digital): /Carta/mi-restaurante</summary>
        [Required, StringLength(50)]
        public string emp_codigo { get; set; }
        [StringLength(11)]
        public string emp_ruc { get; set; }
        public bool emp_activa { get; set; } = true;
        public DateTime emp_creada { get; set; }
        [StringLength(30)]
        public string emp_plan { get; set; } = "basico";
    }

    /// <summary>Registro que pertenece a una empresa: se filtra y se asigna automaticamente</summary>
    public interface IEmpresa
    {
        int id_empresa { get; set; }
    }
}
