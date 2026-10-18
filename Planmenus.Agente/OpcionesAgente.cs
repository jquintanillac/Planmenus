namespace Planmenus.Agente
{
    public class OpcionesAgente
    {
        /// <summary>Direccion del sistema, por ejemplo https://misistema.com</summary>
        public string Servidor { get; set; } = "";
        /// <summary>Clave generada en Configuracion → Ticketera</summary>
        public string Clave { get; set; } = "";
        public int IntervaloSegundos { get; set; } = 3;
        /// <summary>Solo para pruebas: si tiene una carpeta, los tickets se guardan como .bin en lugar de imprimirse</summary>
        public string CarpetaPrueba { get; set; } = "";
    }
}
