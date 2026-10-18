using Planmenus.Data.Entities;
using Planmenus.Models;
using System.Globalization;
using System.Net.Sockets;
using System.Text;

namespace Planmenus.Services
{
    /// <summary>
    /// Genera tickets con comandos ESC/POS (Epson, Xprinter, Bixolon, 3nStar y compatibles)
    /// segun el formato configurado en la pantalla Ticketera, y los envia por red (puerto RAW 9100).
    /// </summary>
    public class EscPosService
    {
        private static readonly CultureInfo Peru = CultureInfo.GetCultureInfo("es-PE");
        private const int FilasPorBanda = 128;

        static EscPosService()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        /// <summary>Paginas de codigos soportadas: codigo .NET y numero ESC t de la impresora</summary>
        public static readonly IReadOnlyDictionary<int, (byte escT, string nombre)> PaginasCodigo = new Dictionary<int, (byte, string)>
        {
            [850] = (2, "PC850 Multilingüe (recomendada)"),
            [858] = (19, "PC858 (con símbolo €)"),
            [1252] = (16, "WPC1252 Windows Latin"),
        };

        public async Task ImprimirAsync(VMTicket t, string ip, int puerto, bool abrirGaveta = false, CancellationToken ct = default)
        {
            await EnviarAsync(Generar(t, abrirGaveta), ip, puerto, ct);
        }

        public async Task EnviarAsync(byte[] datos, string ip, int puerto, CancellationToken ct = default)
        {
            using var cliente = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            await cliente.ConnectAsync(ip, puerto, timeout.Token);
            using var stream = cliente.GetStream();
            await stream.WriteAsync(datos, timeout.Token);
            await stream.FlushAsync(timeout.Token);
        }

        /// <summary>
        /// Construye los bytes del ticket (todas las copias) con el formato de t.formato
        /// </summary>
        public byte[] Generar(VMTicket t, bool abrirGaveta = false)
        {
            var f = t.formato ?? new MDTicketera();
            var b = new Escritor(f);
            if (abrirGaveta)
            {
                b.Bytes(0x1B, 0x70, 0x00, 0x19, 0xFA); // pulso al conector de la gaveta
            }
            for (int copia = 0; copia < Math.Clamp(f.tk_copias, 1, 5); copia++)
            {
                Ticket(b, t, f);
            }
            return b.ToArray();
        }

        private static void Ticket(Escritor b, VMTicket t, MDTicketera f)
        {
            int cols = Math.Clamp(f.tk_columnas, 24, 64);
            b.Inicializar();

            // ---- Cabecera ----
            b.Alinear(1);
            if (f.tk_logo_mostrar && f.tk_logo_raster?.Length > 0 && f.tk_logo_ancho > 0)
            {
                b.Logo(f.tk_logo_raster, f.tk_logo_ancho, f.tk_logo_alto);
            }
            b.Negrita(true);
            b.Tamano(0x11);
            b.Linea(t.empresa);
            b.Tamano(0x00);
            b.Negrita(false);
            if (f.tk_mostrar_ruc && !string.IsNullOrWhiteSpace(t.ruc)) b.Linea($"RUC: {t.ruc}");
            if (f.tk_mostrar_direccion && !string.IsNullOrWhiteSpace(t.direccion)) b.Envolver(t.direccion, cols);
            if (f.tk_mostrar_telefono && !string.IsNullOrWhiteSpace(t.telefono)) b.Linea($"Tel: {t.telefono}");

            if (!string.IsNullOrWhiteSpace(t.comprobante))
            {
                b.Linea(new string('-', cols));
                b.Negrita(true);
                b.Envolver(t.comprobante, cols);
                b.Negrita(false);
            }

            b.Alinear(0);
            b.Linea(new string('-', cols));
            b.Negrita(true);
            b.Linea(string.IsNullOrWhiteSpace(t.comprobante) ? $"{(t.pagado ? "TICKET" : "PRE-CUENTA")} N° {t.ticket:D6}" : $"Pedido N° {t.ticket:D6}");
            b.Negrita(false);
            b.Linea($"Fecha: {t.fecha:dd/MM/yyyy HH:mm}");
            if (!string.IsNullOrWhiteSpace(t.cliente)) b.Envolver($"Cliente: {t.cliente}", cols);
            if (!string.IsNullOrWhiteSpace(t.cliente_doc)) b.Linea(t.cliente_doc);
            if (f.tk_mostrar_mesa && !string.IsNullOrWhiteSpace(t.mesa)) b.Linea($"Mesa: {t.mesa}");
            if (f.tk_mostrar_mesa && !string.IsNullOrWhiteSpace(t.delivery)) b.Linea($"Reparto: {t.delivery}");
            if (f.tk_mostrar_mozo && !string.IsNullOrWhiteSpace(t.usuario)) b.Linea($"Atendió: {t.usuario}");
            if (t.anulado)
            {
                b.Alinear(1);
                b.Negrita(true);
                b.Linea("*** ANULADO ***");
                b.Negrita(false);
                b.Alinear(0);
            }

            // ---- Detalle ----
            b.Linea(new string('-', cols));
            b.Linea(Columnas("CANT DESCRIPCION", "IMPORTE", cols));
            b.Linea(new string('-', cols));
            foreach (var item in t.items)
            {
                b.Linea(Columnas($"{item.cantidad,3} {item.descripcion}", Soles(item.subtotal), cols));
                if (item.cantidad > 1) b.Linea($"     {item.cantidad} x {Soles(item.precio)}");
            }
            b.Linea(new string('-', cols));

            // ---- Totales ----
            foreach (var (etiqueta, monto) in t.subtotales)
            {
                b.Linea(Columnas(etiqueta, Soles(monto), cols));
            }
            b.Negrita(true);
            if (f.tk_total_grande) b.Tamano(0x01); // doble alto, mismo ancho de columnas
            b.Linea(Columnas("TOTAL", Soles(t.total), cols));
            b.Tamano(0x00);
            b.Negrita(false);
            if (t.pagado && !string.IsNullOrWhiteSpace(t.tipo_pago)) b.Linea($"Pago: {t.tipo_pago}");
            if (!t.pagado) b.Linea("PENDIENTE DE PAGO");
            if (f.tk_mostrar_obs && !string.IsNullOrWhiteSpace(t.observacion)) b.Envolver($"Obs: {t.observacion}", cols);

            // ---- Pie ----
            b.Alinear(1);
            if (!string.IsNullOrWhiteSpace(t.qr))
            {
                b.Linea("");
                b.Qr(t.qr);
            }
            foreach (var leyenda in t.leyendas)
            {
                b.Envolver(leyenda, cols);
            }
            b.Linea("");
            b.Envolver(t.pie, cols);
            if (string.IsNullOrWhiteSpace(t.comprobante)) b.Linea("Comprobante interno");

            b.Avanzar(Math.Clamp(f.tk_avance, 0, 10));
            if (f.tk_corte == TipoCorte.Total) b.Bytes(0x1D, 0x56, 0x41, 0x00);
            else if (f.tk_corte == TipoCorte.Parcial) b.Bytes(0x1D, 0x56, 0x42, 0x00);
        }

        private static string Soles(decimal monto) => "S/ " + monto.ToString("N2", Peru);

        /// <summary>Texto a la izquierda e importe a la derecha; si no entra, el texto pasa a varias lineas</summary>
        public static string Columnas(string izquierda, string derecha, int cols)
        {
            int espacio = cols - derecha.Length - 1;
            var lineas = new List<string>();
            while (izquierda.Length > cols)
            {
                lineas.Add(izquierda[..cols]);
                izquierda = "    " + izquierda[cols..];
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

        /// <summary>Acumula los bytes ESC/POS del ticket</summary>
        private sealed class Escritor
        {
            private readonly List<byte> _b = new();
            private readonly Encoding _encoding;
            private readonly byte _escT;

            public Escritor(MDTicketera f)
            {
                var codigo = PaginasCodigo.ContainsKey(f.tk_codepage) ? f.tk_codepage : 850;
                _encoding = Encoding.GetEncoding(codigo);
                _escT = PaginasCodigo[codigo].escT;
            }

            public byte[] ToArray() => _b.ToArray();
            public void Bytes(params byte[] datos) => _b.AddRange(datos);
            public void Inicializar() { Bytes(0x1B, 0x40); Bytes(0x1B, 0x74, _escT); }
            public void Alinear(byte modo) => Bytes(0x1B, 0x61, modo);
            public void Negrita(bool activa) => Bytes(0x1B, 0x45, (byte)(activa ? 1 : 0));
            public void Tamano(byte n) => Bytes(0x1D, 0x21, n);
            public void Avanzar(int lineas) => Bytes(0x1B, 0x64, (byte)lineas);

            public void Linea(string texto)
            {
                _b.AddRange(_encoding.GetBytes(texto ?? ""));
                _b.Add(0x0A);
            }

            /// <summary>Parte el texto en lineas sin cortar palabras</summary>
            public void Envolver(string texto, int cols)
            {
                var linea = new StringBuilder();
                foreach (var palabra in (texto ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (linea.Length > 0 && linea.Length + 1 + palabra.Length > cols)
                    {
                        Linea(linea.ToString());
                        linea.Clear();
                    }
                    if (linea.Length > 0) linea.Append(' ');
                    linea.Append(palabra.Length > cols ? palabra[..cols] : palabra);
                }
                Linea(linea.ToString());
            }

            /// <summary>Imagen de 1 bit con GS v 0, en bandas para no saturar el buffer de la impresora</summary>
            public void Logo(byte[] raster, int ancho, int alto)
            {
                int bytesFila = (ancho + 7) / 8;
                if (raster.Length < bytesFila * alto)
                {
                    return;
                }
                for (int y = 0; y < alto; y += FilasPorBanda)
                {
                    int filas = Math.Min(FilasPorBanda, alto - y);
                    Bytes(0x1D, 0x76, 0x30, 0x00, (byte)(bytesFila & 0xFF), (byte)(bytesFila >> 8), (byte)(filas & 0xFF), (byte)(filas >> 8));
                    _b.AddRange(new ArraySegment<byte>(raster, y * bytesFila, filas * bytesFila));
                }
                _b.Add(0x0A);
            }

            /// <summary>Codigo QR nativo de la impresora (GS ( k), modelo 2, correccion M</summary>
            public void Qr(string texto)
            {
                var datos = Encoding.UTF8.GetBytes(texto);
                int largo = datos.Length + 3;
                Bytes(0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00);           // modelo 2
                Bytes(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, 0x05);                 // tamano del modulo
                Bytes(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, 0x31);                 // correccion M
                Bytes(0x1D, 0x28, 0x6B, (byte)(largo & 0xFF), (byte)(largo >> 8), 0x31, 0x50, 0x30);
                _b.AddRange(datos);
                Bytes(0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30);                 // imprimir
                _b.Add(0x0A);
            }
        }
    }
}
