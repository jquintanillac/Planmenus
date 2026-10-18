using Planmenus.Models;
using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace Planmenus.Services
{
    /// <summary>
    /// Impresion directa en ticketeras termicas compatibles con ESC/POS (Epson, Xprinter, Bixolon...)
    /// a traves de la red (puerto RAW 9100). Solo funciona si el servidor web tiene acceso a la
    /// impresora, por ejemplo cuando la aplicacion corre en una PC del mismo local.
    /// </summary>
    public class EscPosService
    {
        private static readonly CultureInfo Peru = CultureInfo.GetCultureInfo("es-PE");
        private static readonly byte[] Inicializar = { 0x1B, 0x40 };
        private static readonly byte[] PaginaPc850 = { 0x1B, 0x74, 0x02 };
        private static readonly byte[] Cortar = { 0x1D, 0x56, 0x42, 0x00 };

        private readonly Encoding _pc850;

        static EscPosService()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public EscPosService()
        {
            _pc850 = Encoding.GetEncoding(850);
        }

        public async Task ImprimirAsync(VMTicket t, string ip, int puerto, CancellationToken ct = default)
        {
            var datos = Generar(t);
            using var cliente = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            await cliente.ConnectAsync(ip, puerto, timeout.Token);
            using var stream = cliente.GetStream();
            await stream.WriteAsync(datos, timeout.Token);
            await stream.FlushAsync(timeout.Token);
        }

        /// <summary>
        /// Construye los bytes ESC/POS del ticket (48 columnas en 80 mm, 32 en 58 mm)
        /// </summary>
        public byte[] Generar(VMTicket t)
        {
            int cols = t.ancho_mm == 58 ? 32 : 48;
            var b = new List<byte>();
            b.AddRange(Inicializar);
            b.AddRange(PaginaPc850);

            Alinear(b, 1);
            Negrita(b, true);
            Tamano(b, 0x11);
            Linea(b, t.empresa);
            Tamano(b, 0x00);
            Negrita(b, false);
            if (!string.IsNullOrWhiteSpace(t.ruc)) Linea(b, $"RUC: {t.ruc}");
            if (!string.IsNullOrWhiteSpace(t.direccion)) Linea(b, t.direccion);
            if (!string.IsNullOrWhiteSpace(t.telefono)) Linea(b, $"Tel: {t.telefono}");

            Alinear(b, 0);
            Linea(b, new string('-', cols));
            Negrita(b, true);
            Linea(b, $"TICKET N° {t.ticket:D6}");
            Negrita(b, false);
            Linea(b, $"Fecha: {t.fecha:dd/MM/yyyy HH:mm}");
            if (!string.IsNullOrWhiteSpace(t.mesa)) Linea(b, $"Mesa: {t.mesa}");
            if (!string.IsNullOrWhiteSpace(t.delivery)) Linea(b, $"Reparto: {t.delivery}");
            if (!string.IsNullOrWhiteSpace(t.usuario)) Linea(b, $"Atendió: {t.usuario}");
            if (t.anulado)
            {
                Alinear(b, 1);
                Negrita(b, true);
                Linea(b, "*** ANULADO ***");
                Negrita(b, false);
                Alinear(b, 0);
            }
            Linea(b, new string('-', cols));

            foreach (var item in t.items)
            {
                string importe = Soles(item.subtotal);
                string izquierda = $"{item.cantidad} {item.descripcion}";
                Linea(b, Columnas(izquierda, importe, cols));
                if (item.cantidad > 1) Linea(b, $"   {item.cantidad} x {Soles(item.precio)}");
            }

            Linea(b, new string('-', cols));
            Negrita(b, true);
            Tamano(b, 0x01);
            Linea(b, Columnas("TOTAL", Soles(t.total), cols));
            Tamano(b, 0x00);
            Negrita(b, false);
            if (!string.IsNullOrWhiteSpace(t.tipo_pago)) Linea(b, $"Pago: {t.tipo_pago}");
            if (!string.IsNullOrWhiteSpace(t.observacion)) Linea(b, $"Obs: {t.observacion}");

            Alinear(b, 1);
            b.Add(0x0A);
            Linea(b, t.pie);
            Linea(b, "Comprobante interno");
            b.AddRange(new byte[] { 0x1B, 0x64, 0x04 }); // avanzar 4 lineas
            b.AddRange(Cortar);
            return b.ToArray();
        }

        private static string Soles(decimal monto) => "S/ " + monto.ToString("N2", Peru);

        /// <summary>
        /// Texto a la izquierda e importe a la derecha; si no entra, el texto pasa a varias lineas
        /// </summary>
        private static string Columnas(string izquierda, string derecha, int cols)
        {
            int espacio = cols - derecha.Length - 1;
            var lineas = new List<string>();
            while (izquierda.Length > cols)
            {
                lineas.Add(izquierda[..cols]);
                izquierda = izquierda[cols..];
            }
            if (izquierda.Length <= espacio)
            {
                lineas.Add(izquierda.PadRight(espacio) + " " + derecha);
            }
            else
            {
                lineas.Add(izquierda);
                lineas.Add(derecha.PadLeft(cols));
            }
            return string.Join('\n', lineas);
        }

        private void Linea(List<byte> b, string texto)
        {
            b.AddRange(_pc850.GetBytes(texto ?? ""));
            b.Add(0x0A);
        }

        private static void Alinear(List<byte> b, byte modo) => b.AddRange(new byte[] { 0x1B, 0x61, modo });

        private static void Negrita(List<byte> b, bool activa) => b.AddRange(new byte[] { 0x1B, 0x45, (byte)(activa ? 1 : 0) });

        private static void Tamano(List<byte> b, byte n) => b.AddRange(new byte[] { 0x1D, 0x21, n });
    }
}
