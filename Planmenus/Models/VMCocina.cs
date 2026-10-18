namespace Planmenus.Models
{
    public class VMCocina
    {
        public DateTime ahora { get; set; }
        public List<VMComanda> comandas { get; set; } = new();
    }

    public class VMComanda
    {
        public int id_caja { get; set; }
        public DateTime desde { get; set; }
        public byte estado { get; set; }
        public string mesa { get; set; }
        public string delivery { get; set; }
        public string obs { get; set; }
        public bool pagado { get; set; }
        public List<VMTicketItem> platos { get; set; } = new();
    }
}
