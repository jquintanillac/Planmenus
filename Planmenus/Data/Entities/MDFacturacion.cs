using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    /// <summary>
    /// Configuracion de la facturacion electronica (un solo registro).
    /// Las claves se guardan cifradas con ASP.NET Data Protection.
    /// </summary>
    [Table("Facturacion")]
    public class MDFacturacion : IEmpresa
    {
        public int id_empresa { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_fe { get; set; }

        [Display(Name = "Activar facturación electrónica")]
        public bool fe_activa { get; set; }
        /// <summary>Ver <see cref="ProveedorFe"/></summary>
        public byte fe_proveedor { get; set; } = ProveedorFe.SunatDirecto;
        /// <summary>Ver <see cref="AmbienteFe"/></summary>
        public byte fe_ambiente { get; set; } = AmbienteFe.Pruebas;

        // ---- Emisor ----
        [StringLength(11)]
        public string fe_ruc { get; set; }
        [StringLength(200)]
        public string fe_razon_social { get; set; }
        [StringLength(200)]
        public string fe_nombre_comercial { get; set; }
        [StringLength(200)]
        public string fe_direccion { get; set; }
        /// <summary>Ubigeo INEI de 6 digitos del domicilio fiscal</summary>
        [StringLength(6)]
        public string fe_ubigeo { get; set; }
        /// <summary>Codigo de establecimiento anexo (0000 = domicilio fiscal)</summary>
        [StringLength(4)]
        public string fe_establecimiento { get; set; } = "0000";

        // ---- Credenciales SUNAT (cifradas) ----
        [StringLength(50)]
        public string fe_usuario_sol { get; set; }
        [StringLength(500)]
        public string fe_clave_sol { get; set; }
        public byte[] fe_certificado { get; set; }
        [StringLength(500)]
        public string fe_certificado_clave { get; set; }
        [StringLength(300)]
        public string fe_certificado_titular { get; set; }
        public DateTime? fe_certificado_vence { get; set; }

        // ---- Series y tributos ----
        [StringLength(4)]
        public string fe_serie_boleta { get; set; } = "B001";
        [StringLength(4)]
        public string fe_serie_factura { get; set; } = "F001";
        [StringLength(4)]
        public string fe_serie_nc_boleta { get; set; } = "BC01";
        [StringLength(4)]
        public string fe_serie_nc_factura { get; set; } = "FC01";
        /// <summary>Tasa del IGV en porcentaje (18, o la tasa especial de restaurantes MYPE que indique su contador)</summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal fe_igv { get; set; } = 18m;
        /// <summary>Comprobante que aparece seleccionado al cobrar: 0 ticket, 3 boleta, 1 factura</summary>
        public byte fe_por_defecto { get; set; }
    }

    public static class ProveedorFe
    {
        /// <summary>SEE - Del contribuyente: envio directo a SUNAT</summary>
        public const byte SunatDirecto = 0;
    }

    public static class AmbienteFe
    {
        public const byte Pruebas = 0;
        public const byte Produccion = 1;
    }

    /// <summary>Codigos del catalogo 01 de SUNAT</summary>
    public static class TipoComprobante
    {
        public const string Ticket = "00";
        public const string Factura = "01";
        public const string Boleta = "03";
        public const string NotaCredito = "07";

        public static string Nombre(string tipo) => tipo switch
        {
            Factura => "FACTURA ELECTRÓNICA",
            Boleta => "BOLETA DE VENTA ELECTRÓNICA",
            NotaCredito => "NOTA DE CRÉDITO ELECTRÓNICA",
            _ => "TICKET"
        };
    }

    /// <summary>Estado del comprobante ante SUNAT</summary>
    public static class EstadoComprobante
    {
        public const byte PorEnviar = 0;
        public const byte Aceptado = 1;
        public const byte Observado = 2;
        public const byte Rechazado = 3;
        public const byte ErrorEnvio = 4;
        public const byte Anulado = 5;

        public static string Nombre(byte e) => e switch
        {
            PorEnviar => "Por enviar",
            Aceptado => "Aceptado",
            Observado => "Aceptado con observaciones",
            Rechazado => "Rechazado",
            ErrorEnvio => "Error de envío",
            Anulado => "Anulado",
            _ => "?"
        };
    }

    [Table("Comprobante")]
    public class MDComprobante : IEmpresa
    {
        public int id_empresa { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_comp { get; set; }
        public int? id_caja { get; set; }
        [StringLength(2)]
        public string comp_tipo { get; set; }
        [StringLength(4)]
        public string comp_serie { get; set; }
        public int comp_numero { get; set; }
        public DateTime comp_fecha { get; set; }

        /// <summary>Catalogo 06: 1 DNI, 6 RUC, 4 carne de extranjeria, 7 pasaporte, 0 sin documento</summary>
        [StringLength(1)]
        public string cli_tipo_doc { get; set; }
        [StringLength(15)]
        public string cli_num_doc { get; set; }
        [StringLength(200)]
        public string cli_nombre { get; set; }
        [StringLength(200)]
        public string cli_direccion { get; set; }
        [StringLength(150)]
        public string cli_email { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal comp_gravada { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal comp_igv { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal comp_total { get; set; }
        [Column(TypeName = "decimal(5,2)")]
        public decimal comp_tasa_igv { get; set; }

        /// <summary>Valor resumen (DigestValue de la firma) que va en el QR y la representacion impresa</summary>
        [StringLength(100)]
        public string comp_hash { get; set; }
        public byte comp_estado { get; set; }
        [StringLength(10)]
        public string comp_sunat_codigo { get; set; }
        [StringLength(1000)]
        public string comp_sunat_mensaje { get; set; }
        public byte comp_ambiente { get; set; }
        public byte[] comp_xml { get; set; }
        public byte[] comp_cdr { get; set; }
        public int comp_intentos { get; set; }
        public DateTime? comp_enviado { get; set; }

        // ---- Nota de credito ----
        /// <summary>Comprobante que modifica la nota de credito</summary>
        public int? ref_id_comp { get; set; }
        /// <summary>Catalogo 09 (01 = anulacion de la operacion)</summary>
        [StringLength(2)]
        public string nc_motivo { get; set; }
        [StringLength(250)]
        public string nc_sustento { get; set; }

        [NotMapped]
        public string Numero => $"{comp_serie}-{comp_numero:D8}";
    }

    [Table("Comprobante_Detalle")]
    public class MDComprobante_Detalle : IEmpresa
    {
        public int id_empresa { get; set; }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_compdet { get; set; }
        public int id_comp { get; set; }
        public int item { get; set; }
        [StringLength(250)]
        public string descripcion { get; set; }
        [Column(TypeName = "decimal(18,3)")]
        public decimal cantidad { get; set; }
        /// <summary>Precio unitario con IGV</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal precio { get; set; }
        /// <summary>Valor unitario sin IGV</summary>
        [Column(TypeName = "decimal(18,4)")]
        public decimal valor_unitario { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal valor_venta { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal igv { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal total { get; set; }
    }

    /// <summary>Correlativo de cada serie</summary>
    [Table("Serie")]
    public class MDSerie : IEmpresa
    {
        public int id_empresa { get; set; }
        [Key]
        [StringLength(7)]
        public string serie_clave { get; set; }
        public int serie_ultimo { get; set; }
    }
}
