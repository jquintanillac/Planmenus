using System.IO.Compression;
using System.Net;
using System.Text;
using System.Xml;

namespace Planmenus.Services.Facturacion
{
    /// <summary>
    /// Cliente del servicio web billService de SUNAT (SEE - Del contribuyente): sendBill con WS-Security.
    /// </summary>
    public class SunatClient
    {
        public const string UrlPruebas = "https://e-beta.sunat.gob.pe/ol-ti-itcpfegem-beta/billService";
        public const string UrlProduccion = "https://e-factura.sunat.gob.pe/ol-ti-itcpfegem/billService";

        /// <summary>Credenciales publicas del ambiente de pruebas de SUNAT</summary>
        public const string UsuarioPruebas = "20000000001MODDATOS";
        public const string ClavePruebas = "moddatos";

        private readonly HttpClient _http;

        public SunatClient(HttpClient http)
        {
            _http = http;
            _http.Timeout = TimeSpan.FromSeconds(45);
        }

        /// <summary>
        /// Resultado del envio. Aceptado = CDR con codigo 0 o de observacion (4000+).
        /// Reintentable = no llego a SUNAT o SUNAT tuvo una excepcion (codigos 0100-1999).
        /// </summary>
        public record Respuesta(bool Aceptado, bool Observado, bool Reintentable, string Codigo, string Mensaje, byte[] Cdr);

        public async Task<Respuesta> EnviarAsync(string url, string usuario, string clave, string nombreArchivo, byte[] xml, CancellationToken ct = default)
        {
            var zip = Comprimir(nombreArchivo + ".xml", xml);
            var sobre = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:ser=""http://service.sunat.gob.pe"" xmlns:wsse=""http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd"">
<soapenv:Header><wsse:Security><wsse:UsernameToken><wsse:Username>{WebUtility.HtmlEncode(usuario)}</wsse:Username><wsse:Password>{WebUtility.HtmlEncode(clave)}</wsse:Password></wsse:UsernameToken></wsse:Security></soapenv:Header>
<soapenv:Body><ser:sendBill><fileName>{nombreArchivo}.zip</fileName><contentFile>{Convert.ToBase64String(zip)}</contentFile></ser:sendBill></soapenv:Body>
</soapenv:Envelope>";

            HttpResponseMessage r;
            string cuerpo;
            try
            {
                using var contenido = new StringContent(sobre, Encoding.UTF8, "text/xml");
                contenido.Headers.Add("SOAPAction", "urn:sendBill");
                r = await _http.PostAsync(url, contenido, ct);
                cuerpo = await r.Content.ReadAsStringAsync(ct);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                return new Respuesta(false, false, true, null, "No se pudo conectar con SUNAT: " + ex.Message, null);
            }

            XmlDocument doc;
            try
            {
                doc = new XmlDocument();
                doc.LoadXml(cuerpo);
            }
            catch (XmlException)
            {
                return new Respuesta(false, false, true, ((int)r.StatusCode).ToString(), $"Respuesta inesperada de SUNAT (HTTP {(int)r.StatusCode}).", null);
            }

            var fault = doc.GetElementsByTagName("faultstring");
            if (fault.Count > 0)
            {
                var codigoFault = doc.GetElementsByTagName("faultcode").Count > 0 ? doc.GetElementsByTagName("faultcode")[0].InnerText : "";
                var codigo = new string(codigoFault.Where(char.IsDigit).ToArray());
                if (string.IsNullOrEmpty(codigo)) codigo = new string(fault[0].InnerText.Where(char.IsDigit).Take(4).ToArray());
                int.TryParse(codigo, out var n);
                if (n == 1033)
                {
                    // El numero ya figura en SUNAT: pasa al reenviar un comprobante cuya respuesta se perdio.
                    // El sistema nunca reutiliza numeros, por lo que se considera registrado (con observacion para verificarlo).
                    return new Respuesta(true, true, false, codigo, "SUNAT indica que el comprobante ya estaba registrado. Verifíquelo en la consulta de comprobantes de SUNAT. " + fault[0].InnerText.Trim(), null);
                }
                // 0100-1999: excepcion de SUNAT, el comprobante no fue recibido y se puede volver a enviar
                bool reintentable = n is >= 100 and < 2000;
                return new Respuesta(false, false, reintentable, codigo, fault[0].InnerText.Trim(), null);
            }

            var respuesta = doc.GetElementsByTagName("applicationResponse");
            if (respuesta.Count == 0)
            {
                return new Respuesta(false, false, true, ((int)r.StatusCode).ToString(), "SUNAT no devolvió la constancia (CDR).", null);
            }
            var cdrZip = Convert.FromBase64String(respuesta[0].InnerText);
            return LeerCdr(cdrZip);
        }

        private static Respuesta LeerCdr(byte[] cdrZip)
        {
            using var ms = new MemoryStream(cdrZip);
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);
            var entrada = zip.Entries.FirstOrDefault(e => e.Name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase));
            if (entrada == null)
            {
                return new Respuesta(false, false, true, null, "La constancia de SUNAT está vacía.", cdrZip);
            }
            var doc = new XmlDocument();
            using (var s = entrada.Open())
            {
                doc.Load(s);
            }
            var codigo = doc.GetElementsByTagName("cbc:ResponseCode").Count > 0 ? doc.GetElementsByTagName("cbc:ResponseCode")[0].InnerText : "";
            var descripcion = doc.GetElementsByTagName("cbc:Description").Count > 0 ? doc.GetElementsByTagName("cbc:Description")[0].InnerText : "";
            var notas = doc.GetElementsByTagName("cbc:Note").Cast<XmlNode>().Select(x => x.InnerText).ToList();
            int.TryParse(codigo, out var n);
            bool aceptado = n == 0 || n >= 4000;
            bool observado = n >= 4000 || (n == 0 && notas.Count > 0);
            var mensaje = string.Join(" | ", new[] { descripcion }.Concat(notas).Where(x => !string.IsNullOrWhiteSpace(x)));
            return new Respuesta(aceptado, observado, false, codigo, mensaje, cdrZip);
        }

        private static byte[] Comprimir(string nombre, byte[] contenido)
        {
            using var ms = new MemoryStream();
            using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                var entrada = zip.CreateEntry(nombre, CompressionLevel.Optimal);
                using var s = entrada.Open();
                s.Write(contenido);
            }
            return ms.ToArray();
        }
    }
}
