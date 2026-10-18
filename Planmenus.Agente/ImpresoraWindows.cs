using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Planmenus.Agente
{
    /// <summary>
    /// Impresion RAW por el spooler de Windows: los bytes ESC/POS llegan tal cual a la ticketera,
    /// sin pasar por el driver grafico (asi funcionan corte, gaveta, logo y QR).
    /// </summary>
    public static class ImpresoraWindows
    {
        public static void ImprimirRaw(string impresora, byte[] datos, string documento)
        {
            if (!OpenPrinter(impresora, out var h, IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"No se encontró la impresora \"{impresora}\".");
            }
            try
            {
                var info = new DOC_INFO_1 { pDocName = documento, pDatatype = "RAW" };
                if (!StartDocPrinter(h, 1, info)) throw new Win32Exception(Marshal.GetLastWin32Error());
                try
                {
                    if (!StartPagePrinter(h)) throw new Win32Exception(Marshal.GetLastWin32Error());
                    var ptr = Marshal.AllocHGlobal(datos.Length);
                    try
                    {
                        Marshal.Copy(datos, 0, ptr, datos.Length);
                        if (!WritePrinter(h, ptr, datos.Length, out var escritos) || escritos != datos.Length)
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error(), "La impresora no aceptó todos los datos.");
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr);
                        EndPagePrinter(h);
                    }
                }
                finally
                {
                    EndDocPrinter(h);
                }
            }
            finally
            {
                ClosePrinter(h);
            }
        }

        /// <summary>Impresoras instaladas en el equipo (locales y conectadas por red)</summary>
        public static List<string> Instaladas()
        {
            const int PRINTER_ENUM_LOCAL = 2, PRINTER_ENUM_CONNECTIONS = 4;
            int flags = PRINTER_ENUM_LOCAL | PRINTER_ENUM_CONNECTIONS;
            EnumPrinters(flags, null, 4, IntPtr.Zero, 0, out var necesarios, out _);
            if (necesarios == 0)
            {
                return new List<string>();
            }
            var buffer = Marshal.AllocHGlobal(necesarios);
            try
            {
                if (!EnumPrinters(flags, null, 4, buffer, necesarios, out _, out var cantidad))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                var lista = new List<string>();
                int tamano = Marshal.SizeOf<PRINTER_INFO_4>();
                for (int i = 0; i < cantidad; i++)
                {
                    var info = Marshal.PtrToStructure<PRINTER_INFO_4>(buffer + i * tamano);
                    if (!string.IsNullOrWhiteSpace(info.pPrinterName)) lista.Add(info.pPrinterName);
                }
                return lista;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class DOC_INFO_1
        {
            public string pDocName = "";
            public string? pOutputFile;
            public string pDatatype = "RAW";
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct PRINTER_INFO_4
        {
            public string pPrinterName;
            public string pServerName;
            public uint Attributes;
        }

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "OpenPrinterW")]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "StartDocPrinterW")]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOC_INFO_1 di);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true, EntryPoint = "EnumPrintersW")]
        private static extern bool EnumPrinters(int flags, string? name, int level, IntPtr pPrinterEnum, int cbBuf, out int pcbNeeded, out int pcReturned);
    }
}
