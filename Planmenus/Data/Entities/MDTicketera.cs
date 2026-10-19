using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    /// <summary>
    /// Configuracion de la ticketera y del formato del ticket (un solo registro).
    /// </summary>
    [Table("Ticketera")]
    public class MDTicketera : IEmpresa
    {
        public int id_empresa { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_ticketera { get; set; }

        // ---- Conexion ----
        /// <summary>Ver <see cref="ModoImpresion"/></summary>
        public byte tk_modo { get; set; } = ModoImpresion.Navegador;
        /// <summary>Nombre de la impresora en Windows (modo agente)</summary>
        [StringLength(150)]
        public string tk_impresora { get; set; }
        [StringLength(50)]
        public string tk_ip { get; set; }
        public int tk_puerto { get; set; } = 9100;

        // ---- Papel ----
        public int tk_ancho_mm { get; set; } = 80;
        /// <summary>Caracteres por linea con la letra normal (48 en 80 mm, 32 en 58 mm)</summary>
        public int tk_columnas { get; set; } = 48;
        /// <summary>Pagina de codigos ESC/POS: 850, 858 o 1252</summary>
        public int tk_codepage { get; set; } = 850;

        // ---- Logo ----
        public bool tk_logo_mostrar { get; set; } = true;
        /// <summary>Imagen original reducida (PNG) para la vista HTML y el PDF</summary>
        public byte[] tk_logo_png { get; set; }
        /// <summary>Logo en blanco y negro empaquetado 1 bit por punto (filas de izquierda a derecha)</summary>
        public byte[] tk_logo_raster { get; set; }
        public int tk_logo_ancho { get; set; }
        public int tk_logo_alto { get; set; }

        // ---- Contenido ----
        public bool tk_mostrar_ruc { get; set; } = true;
        public bool tk_mostrar_direccion { get; set; } = true;
        public bool tk_mostrar_telefono { get; set; } = true;
        public bool tk_mostrar_mesa { get; set; } = true;
        public bool tk_mostrar_mozo { get; set; } = true;
        public bool tk_mostrar_obs { get; set; } = true;
        public bool tk_total_grande { get; set; } = true;
        [StringLength(120)]
        public string tk_pie { get; set; }

        // ---- Corte y extras ----
        /// <summary>Ver <see cref="TipoCorte"/></summary>
        public byte tk_corte { get; set; } = TipoCorte.Parcial;
        /// <summary>Lineas en blanco antes del corte</summary>
        public int tk_avance { get; set; } = 4;
        public int tk_copias { get; set; } = 1;
        /// <summary>Abrir la gaveta de dinero al cobrar en efectivo</summary>
        public bool tk_gaveta { get; set; }
        /// <summary>Imprimir automaticamente al cobrar (modos red y agente)</summary>
        public bool tk_auto { get; set; } = true;

        // ---- Agente de impresion ----
        /// <summary>SHA-256 de la clave del agente</summary>
        [StringLength(100)]
        public string tk_agente_clave_hash { get; set; }
        public DateTime? tk_agente_contacto { get; set; }
        [StringLength(100)]
        public string tk_agente_equipo { get; set; }
        /// <summary>Impresoras instaladas que reporto el agente, separadas por |</summary>
        [StringLength(2000)]
        public string tk_agente_impresoras { get; set; }
    }

    public static class ModoImpresion
    {
        public const byte Navegador = 0;
        public const byte Red = 1;
        public const byte Agente = 2;
    }

    public static class TipoCorte
    {
        public const byte Total = 0;
        public const byte Parcial = 1;
        public const byte Ninguno = 2;
    }

    /// <summary>
    /// Trabajo de impresion pendiente para el agente local.
    /// </summary>
    [Table("Impresion_Cola")]
    public class MDImpresion_Cola : IEmpresa
    {
        public int id_empresa { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_trabajo { get; set; }
        public DateTime imp_creado { get; set; }
        [StringLength(30)]
        public string imp_tipo { get; set; }
        public int? id_caja { get; set; }
        [StringLength(150)]
        public string imp_impresora { get; set; }
        /// <summary>Bytes ESC/POS listos para enviar a la impresora</summary>
        public byte[] imp_datos { get; set; }
        /// <summary>Ver <see cref="EstadoImpresion"/></summary>
        public byte imp_estado { get; set; }
        public int imp_intentos { get; set; }
        public DateTime? imp_tomado { get; set; }
        public DateTime? imp_terminado { get; set; }
        [StringLength(500)]
        public string imp_error { get; set; }
    }

    public static class EstadoImpresion
    {
        public const byte Pendiente = 0;
        public const byte Impreso = 1;
        public const byte Error = 2;
    }
}
