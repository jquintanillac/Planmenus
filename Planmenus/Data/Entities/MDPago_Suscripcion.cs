using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    /// <summary>Pago de la suscripcion de una empresa a la plataforma (lo registra el superadministrador)</summary>
    [Table("Pago_Suscripcion")]
    public class MDPago_Suscripcion : IEmpresa
    {
        public int id_empresa { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_pago { get; set; }
        public DateTime pago_fecha { get; set; }
        public decimal pago_monto { get; set; }
        public int pago_meses { get; set; }
        [StringLength(30)]
        public string pago_plan { get; set; }
        [StringLength(30)]
        public string pago_medio { get; set; }
        [StringLength(100)]
        public string pago_referencia { get; set; }
        /// <summary>Vencimiento de la suscripcion despues de este pago</summary>
        public DateTime pago_vence { get; set; }
        [StringLength(50)]
        public string pago_usuario { get; set; }
        /// <summary>Id del pago en Mercado Pago (unico: un pago nunca se aplica dos veces)</summary>
        [StringLength(30)]
        public string pago_mp_id { get; set; }
        /// <summary>Ya se emitio su comprobante (si correspondia) y se envio el recibo por correo</summary>
        public bool pago_procesado { get; set; }
        /// <summary>Comprobante electronico emitido por la plataforma, ej. "B001-00000012"</summary>
        [StringLength(20)]
        public string pago_comprobante { get; set; }
        /// <summary>Por que no se emitio el comprobante o no se envio el recibo</summary>
        [StringLength(300)]
        public string pago_nota { get; set; }
    }
}
