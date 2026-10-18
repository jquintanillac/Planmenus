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
        public List<VMTicketItem> items { get; set; } = new();

        public string empresa { get; set; }
        public string ruc { get; set; }
        public string direccion { get; set; }
        public string telefono { get; set; }
        public string pie { get; set; }
        public int ancho_mm { get; set; } = 80;
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
