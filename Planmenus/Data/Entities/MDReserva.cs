using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Reserva")]
    public class MDReserva
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_reserva { get; set; }
        [Display(Name = "Cliente")]
        [Required, StringLength(100)]
        public string res_cliente { get; set; }
        [Display(Name = "Teléfono")]
        [StringLength(30)]
        public string res_telefono { get; set; }
        [Display(Name = "Correo")]
        [StringLength(150), EmailAddress]
        public string res_email { get; set; }
        /// <summary>Otros interesados que reciben los avisos, separados por coma</summary>
        [Display(Name = "Otros correos")]
        [StringLength(500)]
        public string res_emails_extra { get; set; }
        [Display(Name = "Inicio")]
        public DateTime res_inicio { get; set; }
        [Display(Name = "Fin")]
        public DateTime res_fin { get; set; }
        [Display(Name = "Personas")]
        [Range(1, 500)]
        public int res_personas { get; set; } = 2;
        /// <summary>Ver <see cref="TipoReserva"/></summary>
        [Display(Name = "Tipo")]
        public byte res_tipo { get; set; }
        /// <summary>Ver <see cref="EstadoReserva"/></summary>
        [Display(Name = "Estado")]
        public byte res_estado { get; set; }
        [Display(Name = "Notas")]
        [StringLength(500)]
        public string res_notas { get; set; }
        [StringLength(50)]
        public string res_usuario { get; set; }
        public DateTime res_creado { get; set; }
        public DateTime? res_confirmacion_enviada { get; set; }
        public DateTime? res_recordatorio_enviado { get; set; }
    }

    [Table("Reserva_Mesa")]
    public class MDReserva_Mesa
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_resmesa { get; set; }
        public int id_reserva { get; set; }
        public int id_mesa { get; set; }
    }

    public static class EstadoReserva
    {
        public const byte Pendiente = 0;
        public const byte Confirmada = 1;
        public const byte Cancelada = 2;
        public const byte NoAsistio = 3;
        public const byte Completada = 4;

        public static string Nombre(byte e) => e switch
        {
            Pendiente => "Pendiente",
            Confirmada => "Confirmada",
            Cancelada => "Cancelada",
            NoAsistio => "No asistió",
            Completada => "Completada",
            _ => "?"
        };

        /// <summary>Estados que ocupan la mesa (cuentan para cruces de horario)</summary>
        public static bool Ocupa(byte e) => e is Pendiente or Confirmada;
    }

    public static class TipoReserva
    {
        public const byte Mesa = 0;
        public const byte Evento = 1;
        public static string Nombre(byte t) => t == Evento ? "Evento privado" : "Reserva de mesa";
    }
}
