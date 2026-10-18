namespace Planmenus.Helpers
{
    /// <summary>
    /// Fecha y hora de Peru, independiente de la zona horaria del servidor
    /// </summary>
    public static class Fechas
    {
        private static readonly TimeZoneInfo ZonaPeru = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

        public static DateTime AhoraPeru() => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaPeru);

        public static DateTime HoyPeru() => AhoraPeru().Date;
    }
}
