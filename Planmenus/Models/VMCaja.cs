using System;

namespace Planmenus.Models
{
    public class VMCaja
    {
        public int ticket { get; set; }
        public string menu { get; set; }
        public decimal monto { get; set; }
        public DateTime fecha { get; set; }
    }
}
