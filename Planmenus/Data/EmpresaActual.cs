namespace Planmenus.Data
{
    /// <summary>
    /// Empresa con la que se trabaja en la peticion actual (o en un proceso en segundo plano).
    /// La establece el middleware a partir del usuario conectado; sin empresa no se ve ningun dato.
    /// </summary>
    public class EmpresaActual
    {
        public int? Id { get; private set; }

        /// <summary>Fin de la suscripcion pagada o de la prueba (null = no vence). Lo carga el middleware.</summary>
        public DateTime? Vence { get; set; }

        public void Establecer(int idEmpresa) => Id = idEmpresa;
    }
}
