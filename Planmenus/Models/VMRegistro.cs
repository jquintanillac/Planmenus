using System.ComponentModel.DataAnnotations;

namespace Planmenus.Models
{
    /// <summary>Formulario publico "Crea tu restaurante"</summary>
    public class VMRegistro
    {
        [Required(ErrorMessage = "Ingrese el nombre de su restaurante.")]
        [StringLength(150)]
        [Display(Name = "Nombre del restaurante")]
        public string nombre { get; set; }

        [StringLength(50)]
        [Display(Name = "Dirección de su carta")]
        public string codigo { get; set; }

        [StringLength(11)]
        [Display(Name = "RUC (opcional)")]
        public string ruc { get; set; }

        [Required(ErrorMessage = "Ingrese su nombre.")]
        [StringLength(100)]
        [Display(Name = "Su nombre")]
        public string admin_nombre { get; set; }

        [Required(ErrorMessage = "Ingrese un correo.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        [StringLength(150)]
        [Display(Name = "Correo")]
        public string email { get; set; }

        [Required(ErrorMessage = "Elija un usuario.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El usuario debe tener entre 3 y 50 caracteres.")]
        [RegularExpression("^[a-zA-Z0-9._-]+$", ErrorMessage = "Use solo letras, números, punto, guion o guion bajo.")]
        [Display(Name = "Usuario para ingresar")]
        public string usuario { get; set; }

        [Required(ErrorMessage = "Elija una contraseña.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string clave { get; set; }

        [Compare(nameof(clave), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Repita la contraseña")]
        public string clave2 { get; set; }

        [Display(Name = "Acepto los términos y la política de privacidad")]
        public bool acepto { get; set; }

        /// <summary>Campo trampa invisible: si llega con datos, lo lleno un robot</summary>
        public string sitio_web { get; set; }
    }

    /// <summary>Asistente inicial despues del registro</summary>
    public class VMBienvenida
    {
        public string direccion { get; set; }
        public string telefono { get; set; }
        [Range(0, 60)]
        public int mesas { get; set; } = 6;
        [Range(1, 20)]
        public int capacidad { get; set; } = 4;
        public List<VMPlatoInicial> platos { get; set; } = new();
    }

    public class VMPlatoInicial
    {
        public string nombre { get; set; }
        public decimal? precio { get; set; }
    }
}
