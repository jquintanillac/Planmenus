namespace Planmenus.Models
{
    /// <summary>
    /// Pedido enviado desde la pantalla de venta. Los precios se toman siempre de la base de datos.
    /// </summary>
    public class VMVenta
    {
        public int id_tipago { get; set; }
        public int? id_mesa { get; set; }
        public int? id_deliv { get; set; }
        public string caja_obs { get; set; }
        public List<VMVentaItem> items { get; set; } = new();
    }

    public class VMVentaItem
    {
        public int id_menu { get; set; }
        public int cantidad { get; set; }
    }
}
