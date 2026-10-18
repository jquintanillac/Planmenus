namespace Planmenus.Models
{
    public class VMSalon
    {
        public DateTime ahora { get; set; }
        public List<VMSalonMesa> mesas { get; set; } = new();
        /// <summary>Pedidos abiertos para llevar o delivery</summary>
        public List<VMSalonPedido> sin_mesa { get; set; } = new();
    }

    public class VMSalonMesa
    {
        public int id_mesa { get; set; }
        public string mesa_desc { get; set; }
        public int mesa_nro { get; set; }
        /// <summary>null = mesa libre</summary>
        public VMSalonPedido pedido { get; set; }
    }

    public class VMSalonPedido
    {
        public int id_caja { get; set; }
        public int? id_mesa { get; set; }
        public decimal monto { get; set; }
        public DateTime desde { get; set; }
        public byte cocina { get; set; }
        public int platos { get; set; }
        public string obs { get; set; }
    }
}
