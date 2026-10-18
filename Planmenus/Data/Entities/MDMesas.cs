using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Planmenus.Data.Entities
{
    [Table("Mesas")]
    public class MDMesas
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id_mesa { get; set; }
        [Display(Name = "Descripcion")]
        public string mesa_desc { get; set; }
        [Display(Name = "Nro. de mesa")]
        public int mesa_nro { get; set; }
        [Display(Name = "Activo")]
        public bool mesa_act { get; set; }

        [Display(Name = "Capacidad (personas)")]
        [Range(1, 50)]
        public int mesa_capacidad { get; set; } = 4;
        [Display(Name = "Zona")]
        public int? id_zona { get; set; }

        // Ubicacion en el plano del salon (unidades del lienzo de 1200 x 800)
        /// <summary>cuadrada, redonda o rectangular</summary>
        [StringLength(15)]
        public string mesa_forma { get; set; } = FormaMesa.Cuadrada;
        public int? mesa_x { get; set; }
        public int? mesa_y { get; set; }
        public int mesa_ancho { get; set; } = 90;
        public int mesa_alto { get; set; } = 90;
        public int mesa_rotacion { get; set; }
    }

    public static class FormaMesa
    {
        public const string Cuadrada = "cuadrada";
        public const string Redonda = "redonda";
        public const string Rectangular = "rectangular";
        public static readonly string[] Todas = { Cuadrada, Redonda, Rectangular };
    }
}
