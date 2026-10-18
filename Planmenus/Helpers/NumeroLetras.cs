namespace Planmenus.Helpers
{
    /// <summary>
    /// Monto en letras para la leyenda de los comprobantes: 1234.50 → "MIL DOSCIENTOS TREINTA Y CUATRO CON 50/100 SOLES".
    /// </summary>
    public static class NumeroLetras
    {
        private static readonly string[] Unidades = { "", "UNO", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE",
            "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISÉIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE",
            "VEINTE", "VEINTIUNO", "VEINTIDÓS", "VEINTITRÉS", "VEINTICUATRO", "VEINTICINCO", "VEINTISÉIS", "VEINTISIETE", "VEINTIOCHO", "VEINTINUEVE" };
        private static readonly string[] Decenas = { "", "", "", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };
        private static readonly string[] Centenas = { "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS" };

        public static string Soles(decimal monto)
        {
            monto = Math.Round(Math.Abs(monto), 2);
            var entero = (long)Math.Truncate(monto);
            var centimos = (int)Math.Round((monto - entero) * 100);
            var texto = entero == 0 ? "CERO" : Convertir(entero);
            return $"{texto} CON {centimos:D2}/100 SOLES";
        }

        private static string Convertir(long n)
        {
            if (n >= 1_000_000)
            {
                var millones = n / 1_000_000;
                var resto = n % 1_000_000;
                var t = millones == 1 ? "UN MILLÓN" : Convertir(millones).Replace("UNO", "UN") + " MILLONES";
                return resto == 0 ? t : t + " " + Convertir(resto);
            }
            if (n >= 1000)
            {
                var miles = n / 1000;
                var resto = n % 1000;
                var t = miles == 1 ? "MIL" : Apocopar(Convertir(miles)) + " MIL";
                return resto == 0 ? t : t + " " + Convertir(resto);
            }
            if (n >= 100)
            {
                if (n == 100) return "CIEN";
                var resto = n % 100;
                return Centenas[n / 100] + (resto == 0 ? "" : " " + Convertir(resto));
            }
            if (n < 30)
            {
                return Unidades[n];
            }
            var u = n % 10;
            return Decenas[n / 10] + (u == 0 ? "" : " Y " + Unidades[u]);
        }

        /// <summary>"VEINTIUNO MIL" → "VEINTIÚN MIL", "UNO MIL" → "UN MIL"</summary>
        private static string Apocopar(string s) =>
            s.EndsWith("VEINTIUNO") ? s[..^"VEINTIUNO".Length] + "VEINTIÚN" : s.EndsWith("UNO") ? s[..^3] + "UN" : s;
    }
}
