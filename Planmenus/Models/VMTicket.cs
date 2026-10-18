namespace Planmenus.Models
{
    /// <summary>
    /// Datos de una venta listos para imprimir (HTML, PDF o ESC/POS)
    /// </summary>
    public class VMTicket
    {
        public int ticket { get; set; }
        public DateTime fecha { get; set; }
        public string usuario { get; set; }
        public string tipo_pago { get; set; }
        public string mesa { get; set; }
        public string delivery { get; set; }
        public string observacion { get; set; }
        public bool anulado { get; set; }
        /// <summary>false = pre-cuenta de un pedido abierto</summary>
        public bool pagado { get; set; } = true;
        public decimal total { get; set; }

        // ---- Comprobante electronico (vacio = ticket interno) ----
        /// <summary>Ej.: "BOLETA DE VENTA ELECTRÓNICA B001-00000123"</summary>
        public string comprobante { get; set; }
        public string cliente { get; set; }
        /// <summary>Ej.: "DNI: 12345678" o "RUC: 20123456789"</summary>
        public string cliente_doc { get; set; }
        /// <summary>Lineas antes del total: Op. gravada, IGV...</summary>
        public List<(string etiqueta, decimal monto)> subtotales { get; set; } = new();
        /// <summary>Texto del codigo QR (comprobante electronico)</summary>
        public string qr { get; set; }
        /// <summary>Textos legales al pie (representacion impresa, monto en letras...)</summary>
        public List<string> leyendas { get; set; } = new();
        public List<VMTicketItem> items { get; set; } = new();

        public string empresa { get; set; }
        public string ruc { get; set; }
        public string direccion { get; set; }
        public string telefono { get; set; }
        public string pie { get; set; }
        public int ancho_mm { get; set; } = 80;
        /// <summary>Formato configurado en la pantalla Ticketera (secciones visibles, logo, corte...)</summary>
        public Planmenus.Data.Entities.MDTicketera formato { get; set; } = new();
    }

    public class VMTicketItem
    {
        public int id_detalle { get; set; }
        public string descripcion { get; set; }
        public int cantidad { get; set; }
        public decimal precio { get; set; }
        public decimal subtotal { get; set; }
    }
}
