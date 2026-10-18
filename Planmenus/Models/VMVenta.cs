namespace Planmenus.Models
{
    /// <summary>
    /// Pedido enviado desde la pantalla de venta. Los precios se toman siempre de la base de datos.
    /// </summary>
    public class VMVenta
    {
        /// <summary>"cobrar" = se cobra en el momento; "cocina" = pedido abierto que se cobra despues</summary>
        public string accion { get; set; } = "cobrar";
        /// <summary>Pedido abierto al que se agregan platos (opcional)</summary>
        public int? id_caja { get; set; }
        public int? id_tipago { get; set; }
        public int? id_mesa { get; set; }
        public int? id_deliv { get; set; }
        public string caja_obs { get; set; }
        public List<VMVentaItem> items { get; set; } = new();

        // ---- Comprobante electronico (si la facturacion esta activa) ----
        /// <summary>00 ticket interno, 03 boleta, 01 factura</summary>
        public string comp_tipo { get; set; } = "00";
        public string cli_tipo_doc { get; set; } = "1";
        public string cli_num_doc { get; set; }
        public string cli_nombre { get; set; }
        public string cli_direccion { get; set; }
        public string cli_email { get; set; }

        public bool EmiteComprobante => comp_tipo == "01" || comp_tipo == "03";

        public bool Cobrar => accion == "cobrar";
    }

    public class VMVentaItem
    {
        public int id_menu { get; set; }
        public int cantidad { get; set; }
    }
}
