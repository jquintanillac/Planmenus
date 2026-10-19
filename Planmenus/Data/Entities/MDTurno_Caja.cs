using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    /// <summary>
    /// Turno de caja: apertura con fondo inicial y cierre con arqueo del efectivo contado.
    /// </summary>
    [Table("Turno_Caja")]
    public class MDTurno_Caja : IEmpresa
    {
        public int id_empresa { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_turno { get; set; }
        [Display(Name = "Abierto por")]
        [StringLength(50)]
        public string turno_usuario_apertura { get; set; }
        [Display(Name = "Apertura")]
        public DateTime turno_apertura { get; set; }
        [Display(Name = "Fondo inicial")]
        public decimal turno_monto_inicial { get; set; }
        [Display(Name = "Cerrado por")]
        [StringLength(50)]
        public string turno_usuario_cierre { get; set; }
        [Display(Name = "Cierre")]
        public DateTime? turno_cierre { get; set; }
        [Display(Name = "Efectivo esperado")]
        public decimal? turno_esperado { get; set; }
        [Display(Name = "Efectivo contado")]
        public decimal? turno_contado { get; set; }
        [Display(Name = "Diferencia")]
        public decimal? turno_diferencia { get; set; }
        [Display(Name = "Observación")]
        [StringLength(300)]
        public string turno_obs { get; set; }
        [Display(Name = "Abierto")]
        public bool turno_abierto { get; set; }
    }
}
