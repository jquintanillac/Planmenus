namespace Planmenus.Models
{
    /// <summary>
    /// Indicadores del dia para la pantalla de inicio
    /// </summary>
    public class VMDashboard
    {
        public decimal ventas { get; set; }
        public int tickets { get; set; }
        public decimal ticket_promedio => tickets == 0 ? 0 : Math.Round(ventas / tickets, 2);
        public int anulados { get; set; }
        public decimal ventas_ayer { get; set; }
        public int platos_programados { get; set; }
        public List<VMDashboardFila> top_platos { get; set; } = new();
        public List<VMDashboardFila> por_tipo_pago { get; set; } = new();
        public List<VMDashboardFila> por_hora { get; set; } = new();
    }

    public class VMDashboardFila
    {
        public string etiqueta { get; set; }
        public int cantidad { get; set; }
        public decimal monto { get; set; }
    }
}
