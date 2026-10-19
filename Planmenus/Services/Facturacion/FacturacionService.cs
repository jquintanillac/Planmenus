using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Services;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Planmenus.Services.Facturacion
{
    /// <summary>
    /// Emision de boletas, facturas y notas de credito electronicas (SEE - Del contribuyente).
    /// </summary>
    public class FacturacionService
    {
        public const decimal MontoBoletaConDocumento = 700m;

        private readonly Datacontext _context;
        private readonly UblService _ubl;
        private readonly SunatClient _sunat;
        private readonly IDataProtector _protector;
        private readonly ILogger<FacturacionService> _logger;
        private readonly PlanService _planes;

        public FacturacionService(Datacontext context, UblService ubl, SunatClient sunat, IDataProtectionProvider proteccion, ILogger<FacturacionService> logger, PlanService planes)
        {
            _context = context;
            _ubl = ubl;
            _sunat = sunat;
            _protector = proteccion.CreateProtector("Planmenus.Facturacion");
            _logger = logger;
            _planes = planes;
        }

        public record Cliente(string tipo_comprobante, string tipo_doc, string num_doc, string nombre, string direccion, string email);
        public record Resultado(MDComprobante Comprobante, string Error)
        {
            public bool Ok => Error == null;
        }

        // ------------------------------------------------------------------ configuracion

        public async Task<MDFacturacion> ConfigAsync() =>
            await _context.Facturacion.OrderBy(f => f.id_fe).FirstOrDefaultAsync() ?? new MDFacturacion();

        public async Task<bool> ActivaAsync() => (await ConfigAsync()).fe_activa;

        public string Cifrar(string texto) => string.IsNullOrEmpty(texto) ? null : _protector.Protect(texto);

        public string Descifrar(string cifrado)
        {
            if (string.IsNullOrEmpty(cifrado)) return null;
            try { return _protector.Unprotect(cifrado); }
            catch (CryptographicException) { return null; }
        }

        public X509Certificate2 Certificado(MDFacturacion fe)
        {
            if (fe.fe_certificado == null) return null;
            return new X509Certificate2(fe.fe_certificado, Descifrar(fe.fe_certificado_clave),
                X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
        }

        /// <summary>Lo que falta para poder emitir</summary>
        public List<string> Pendientes(MDFacturacion fe)
        {
            var p = new List<string>();
            if (!Documentos.RucValido(fe.fe_ruc)) p.Add("RUC del emisor válido");
            if (string.IsNullOrWhiteSpace(fe.fe_razon_social)) p.Add("razón social");
            if (string.IsNullOrWhiteSpace(fe.fe_direccion)) p.Add("dirección fiscal");
            if (fe.fe_certificado == null) p.Add("certificado digital");
            else if (fe.fe_certificado_vence < Fechas.AhoraPeru()) p.Add("certificado vigente (el actual venció)");
            if (fe.fe_ambiente == AmbienteFe.Produccion && (string.IsNullOrWhiteSpace(fe.fe_usuario_sol) || string.IsNullOrWhiteSpace(fe.fe_clave_sol)))
                p.Add("usuario y clave SOL");
            return p;
        }

        /// <summary>Certificado autofirmado para el ambiente de pruebas de SUNAT</summary>
        public void GenerarCertificadoPrueba(MDFacturacion fe)
        {
            using var rsa = RSA.Create(2048);
            var req = new CertificateRequest($"CN=CERTIFICADO DE PRUEBA {fe.fe_ruc}, O={fe.fe_razon_social?.Replace(",", " ")}, C=PE", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation, true));
            using var cert = req.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(1));
            var clave = Convert.ToBase64String(RandomNumberGenerator.GetBytes(18));
            GuardarCertificado(fe, cert.Export(X509ContentType.Pfx, clave), clave);
        }

        /// <summary>Valida y guarda el certificado .pfx/.p12. Devuelve un error o null.</summary>
        public string GuardarCertificado(MDFacturacion fe, byte[] pfx, string clave)
        {
            try
            {
                using var cert = new X509Certificate2(pfx, clave, X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable);
                if (!cert.HasPrivateKey || cert.GetRSAPrivateKey() == null)
                {
                    return "El certificado no contiene la clave privada RSA.";
                }
                fe.fe_certificado = pfx;
                fe.fe_certificado_clave = Cifrar(clave);
                fe.fe_certificado_titular = cert.GetNameInfo(X509NameType.SimpleName, false);
                fe.fe_certificado_vence = cert.NotAfter;
                return null;
            }
            catch (CryptographicException)
            {
                return "No se pudo abrir el certificado: revise el archivo y la contraseña.";
            }
        }

        // ------------------------------------------------------------------ emision

        /// <summary>Valida los datos del cliente segun el tipo de comprobante</summary>
        public static string ValidarCliente(Cliente c, decimal total)
        {
            if (c.tipo_comprobante == TipoComprobante.Factura)
            {
                if (!Documentos.RucValido(c.num_doc)) return "Para factura ingrese un RUC válido de 11 dígitos.";
                if (string.IsNullOrWhiteSpace(c.nombre)) return "Para factura ingrese la razón social del cliente.";
            }
            else if (c.tipo_comprobante == TipoComprobante.Boleta)
            {
                bool conDoc = !string.IsNullOrWhiteSpace(c.num_doc);
                if (total >= MontoBoletaConDocumento && (!conDoc || string.IsNullOrWhiteSpace(c.nombre)))
                    return $"Las boletas desde S/ {MontoBoletaConDocumento:N0} requieren DNI y nombre del cliente.";
                if (conDoc && c.tipo_doc == "1" && !Documentos.DniValido(c.num_doc)) return "El DNI debe tener 8 dígitos.";
                if (conDoc && c.tipo_doc == "6" && !Documentos.RucValido(c.num_doc)) return "El RUC no es válido.";
            }
            return null;
        }

        /// <summary>Emite el comprobante de una venta cobrada y lo envia a SUNAT</summary>
        public async Task<Resultado> EmitirVentaAsync(int id_caja, Cliente cliente)
        {
            var fe = await ConfigAsync();
            if (!fe.fe_activa)
            {
                return new Resultado(null, "La facturación electrónica no está activa.");
            }
            var faltan = Pendientes(fe);
            if (faltan.Count > 0)
            {
                return new Resultado(null, "Complete la configuración de facturación: " + string.Join(", ", faltan) + ".");
            }
            var caja = await _context.Cajas.AsNoTracking().FirstOrDefaultAsync(c => c.id_caja == id_caja && c.caja_act && c.caja_pagado);
            if (caja == null)
            {
                return new Resultado(null, "La venta no existe o no está cobrada.");
            }
            if (await _context.Comprobantes.AnyAsync(c => c.id_caja == id_caja && c.comp_tipo != TipoComprobante.NotaCredito && c.comp_estado != EstadoComprobante.Rechazado))
            {
                return new Resultado(null, "Esta venta ya tiene un comprobante electrónico.");
            }
            var error = ValidarCliente(cliente, caja.caja_monto);
            if (error != null)
            {
                return new Resultado(null, error);
            }

            var items = await (from d in _context.Caja_Detalles
                               join m in _context.Menus on d.id_menu equals m.id_menu
                               where d.id_caja == id_caja
                               orderby d.id_detalle
                               select new { m.menu_desc, d.det_cant, d.det_precio, d.det_subtotal }).ToListAsync();
            if (items.Count == 0)
            {
                return new Resultado(null, "La venta no tiene platos.");
            }

            var tasa = fe.fe_igv / 100m;
            var lineas = items.Select((i, n) =>
            {
                var valor = Math.Round(i.det_subtotal / (1 + tasa), 2, MidpointRounding.AwayFromZero);
                return new MDComprobante_Detalle
                {
                    item = n + 1,
                    descripcion = PdfService.Limpio(i.menu_desc) ?? "Consumo",
                    cantidad = i.det_cant,
                    precio = i.det_precio,
                    valor_unitario = Math.Round(i.det_precio / (1 + tasa), 4, MidpointRounding.AwayFromZero),
                    valor_venta = valor,
                    igv = i.det_subtotal - valor,
                    total = i.det_subtotal
                };
            }).ToList();

            var esFactura = cliente.tipo_comprobante == TipoComprobante.Factura;
            var conDoc = !string.IsNullOrWhiteSpace(cliente.num_doc);
            var comp = new MDComprobante
            {
                id_caja = id_caja,
                comp_tipo = esFactura ? TipoComprobante.Factura : TipoComprobante.Boleta,
                comp_serie = esFactura ? fe.fe_serie_factura : fe.fe_serie_boleta,
                comp_fecha = caja.caja_fecha_pago ?? Fechas.AhoraPeru(),
                cli_tipo_doc = esFactura ? "6" : conDoc ? (cliente.tipo_doc ?? "1") : "0",
                cli_num_doc = conDoc ? cliente.num_doc.Trim() : "-",
                cli_nombre = string.IsNullOrWhiteSpace(cliente.nombre) ? "CLIENTES VARIOS" : PdfService.Limpio(cliente.nombre.Trim()).ToUpperInvariant(),
                cli_direccion = PdfService.Limpio(cliente.direccion?.Trim()),
                cli_email = cliente.email?.Trim(),
                comp_gravada = lineas.Sum(l => l.valor_venta),
                comp_igv = lineas.Sum(l => l.igv),
                comp_total = lineas.Sum(l => l.total),
                comp_tasa_igv = fe.fe_igv,
                comp_ambiente = fe.fe_ambiente,
                comp_estado = EstadoComprobante.PorEnviar
            };
            return await EmitirAsync(fe, comp, lineas, null);
        }

        /// <summary>Boleta de prueba (sin venta asociada) para validar la configuracion en el ambiente de pruebas</summary>
        public async Task<Resultado> EmitirPruebaAsync()
        {
            var fe = await ConfigAsync();
            if (fe.fe_ambiente != AmbienteFe.Pruebas)
            {
                return new Resultado(null, "La boleta de prueba solo se puede emitir en el ambiente de pruebas.");
            }
            var faltan = Pendientes(fe);
            if (faltan.Count > 0)
            {
                return new Resultado(null, "Complete la configuración de facturación: " + string.Join(", ", faltan) + ".");
            }
            var tasa = fe.fe_igv / 100m;
            var valor = Math.Round(10m / (1 + tasa), 2, MidpointRounding.AwayFromZero);
            var lineas = new List<MDComprobante_Detalle>
            {
                new() { item = 1, descripcion = "PRODUCTO DE PRUEBA", cantidad = 1, precio = 10m, valor_unitario = Math.Round(10m / (1 + tasa), 4), valor_venta = valor, igv = 10m - valor, total = 10m }
            };
            var comp = new MDComprobante
            {
                comp_tipo = TipoComprobante.Boleta,
                comp_serie = fe.fe_serie_boleta,
                comp_fecha = Fechas.AhoraPeru(),
                cli_tipo_doc = "0", cli_num_doc = "-", cli_nombre = "CLIENTES VARIOS",
                comp_gravada = valor, comp_igv = 10m - valor, comp_total = 10m, comp_tasa_igv = fe.fe_igv,
                comp_ambiente = AmbienteFe.Pruebas, comp_estado = EstadoComprobante.PorEnviar
            };
            return await EmitirAsync(fe, comp, lineas, null);
        }

        /// <summary>
        /// Comprobante por un servicio sin venta de caja (por ejemplo, la suscripcion que la plataforma cobra
        /// a un restaurante). El total incluye IGV. Factura si el cliente tiene RUC, si no boleta.
        /// </summary>
        public async Task<Resultado> EmitirServicioAsync(string descripcion, decimal total, Cliente cliente)
        {
            var fe = await ConfigAsync();
            if (!fe.fe_activa)
            {
                return new Resultado(null, "La facturación electrónica no está activa.");
            }
            var faltan = Pendientes(fe);
            if (faltan.Count > 0)
            {
                return new Resultado(null, "Complete la configuración de facturación: " + string.Join(", ", faltan) + ".");
            }
            var error = ValidarCliente(cliente, total);
            if (error != null)
            {
                return new Resultado(null, error);
            }

            var tasa = fe.fe_igv / 100m;
            var valor = Math.Round(total / (1 + tasa), 2, MidpointRounding.AwayFromZero);
            var lineas = new List<MDComprobante_Detalle>
            {
                new()
                {
                    item = 1, descripcion = PdfService.Limpio(descripcion), cantidad = 1, precio = total,
                    valor_unitario = Math.Round(total / (1 + tasa), 4, MidpointRounding.AwayFromZero), valor_venta = valor, igv = total - valor, total = total
                }
            };
            var esFactura = cliente.tipo_comprobante == TipoComprobante.Factura;
            var conDoc = !string.IsNullOrWhiteSpace(cliente.num_doc);
            var comp = new MDComprobante
            {
                comp_tipo = esFactura ? TipoComprobante.Factura : TipoComprobante.Boleta,
                comp_serie = esFactura ? fe.fe_serie_factura : fe.fe_serie_boleta,
                comp_fecha = Fechas.AhoraPeru(),
                cli_tipo_doc = esFactura ? "6" : conDoc ? (cliente.tipo_doc ?? "1") : "0",
                cli_num_doc = conDoc ? cliente.num_doc.Trim() : "-",
                cli_nombre = string.IsNullOrWhiteSpace(cliente.nombre) ? "CLIENTES VARIOS" : PdfService.Limpio(cliente.nombre.Trim()).ToUpperInvariant(),
                cli_direccion = PdfService.Limpio(cliente.direccion?.Trim()),
                cli_email = cliente.email?.Trim(),
                comp_gravada = valor, comp_igv = total - valor, comp_total = total, comp_tasa_igv = fe.fe_igv,
                comp_ambiente = fe.fe_ambiente, comp_estado = EstadoComprobante.PorEnviar
            };
            return await EmitirAsync(fe, comp, lineas, null);
        }

        /// <summary>Anula un comprobante aceptado emitiendo una nota de credito por el total (motivo 01)</summary>
        public async Task<Resultado> AnularAsync(int id_comp, string sustento)
        {
            var fe = await ConfigAsync();
            var original = await _context.Comprobantes.FirstOrDefaultAsync(c => c.id_comp == id_comp);
            if (original == null || original.comp_tipo == TipoComprobante.NotaCredito)
            {
                return new Resultado(null, "Comprobante no encontrado.");
            }
            if (original.comp_estado == EstadoComprobante.Anulado)
            {
                return new Resultado(original, "El comprobante ya está anulado.");
            }
            if (original.comp_estado == EstadoComprobante.Rechazado)
            {
                // Un comprobante rechazado no tiene validez: basta con marcarlo
                original.comp_estado = EstadoComprobante.Anulado;
                await _context.SaveChangesAsync();
                return new Resultado(original, null);
            }
            if (original.comp_estado is EstadoComprobante.PorEnviar or EstadoComprobante.ErrorEnvio)
            {
                return new Resultado(original, "Primero reenvíe el comprobante a SUNAT; cuando esté aceptado podrá anularlo con nota de crédito.");
            }
            var faltan = Pendientes(fe);
            if (faltan.Count > 0)
            {
                return new Resultado(null, "Complete la configuración de facturación: " + string.Join(", ", faltan) + ".");
            }

            var lineasOriginal = await _context.Comprobante_Detalles.AsNoTracking().Where(d => d.id_comp == id_comp).OrderBy(d => d.item).ToListAsync();
            var nota = new MDComprobante
            {
                id_caja = original.id_caja,
                comp_tipo = TipoComprobante.NotaCredito,
                comp_serie = original.comp_tipo == TipoComprobante.Factura ? fe.fe_serie_nc_factura : fe.fe_serie_nc_boleta,
                comp_fecha = Fechas.AhoraPeru(),
                cli_tipo_doc = original.cli_tipo_doc,
                cli_num_doc = original.cli_num_doc,
                cli_nombre = original.cli_nombre,
                cli_direccion = original.cli_direccion,
                cli_email = original.cli_email,
                comp_gravada = original.comp_gravada,
                comp_igv = original.comp_igv,
                comp_total = original.comp_total,
                comp_tasa_igv = original.comp_tasa_igv,
                comp_ambiente = fe.fe_ambiente,
                comp_estado = EstadoComprobante.PorEnviar,
                ref_id_comp = original.id_comp,
                nc_motivo = "01",
                nc_sustento = string.IsNullOrWhiteSpace(sustento) ? "ANULACION DE LA OPERACION" : PdfService.Limpio(sustento.Trim()).ToUpperInvariant()
            };
            var lineas = lineasOriginal.Select(l => new MDComprobante_Detalle
            {
                item = l.item, descripcion = l.descripcion, cantidad = l.cantidad, precio = l.precio,
                valor_unitario = l.valor_unitario, valor_venta = l.valor_venta, igv = l.igv, total = l.total
            }).ToList();

            var r = await EmitirAsync(fe, nota, lineas, original);
            if (r.Comprobante?.comp_estado is EstadoComprobante.Aceptado or EstadoComprobante.Observado)
            {
                original.comp_estado = EstadoComprobante.Anulado;
                await _context.SaveChangesAsync();
            }
            return r;
        }

        /// <summary>Vuelve a enviar un comprobante que no llego a SUNAT</summary>
        public async Task<Resultado> ReenviarAsync(int id_comp)
        {
            var comp = await _context.Comprobantes.FirstOrDefaultAsync(c => c.id_comp == id_comp);
            if (comp == null) return new Resultado(null, "Comprobante no encontrado.");
            if (comp.comp_estado is not (EstadoComprobante.PorEnviar or EstadoComprobante.ErrorEnvio))
            {
                return new Resultado(comp, "Solo se reenvían comprobantes pendientes o con error de envío.");
            }
            var r = await EnviarAsync(await ConfigAsync(), comp);
            if (r.Ok && comp.ref_id_comp != null && comp.comp_estado is EstadoComprobante.Aceptado or EstadoComprobante.Observado)
            {
                var original = await _context.Comprobantes.FindAsync(comp.ref_id_comp);
                if (original != null) original.comp_estado = EstadoComprobante.Anulado;
                await _context.SaveChangesAsync();
            }
            return r;
        }

        public Task<List<int>> PorReenviarAsync() =>
            _context.Comprobantes
                .Where(c => (c.comp_estado == EstadoComprobante.PorEnviar || c.comp_estado == EstadoComprobante.ErrorEnvio) && c.comp_intentos < 20)
                .OrderBy(c => c.id_comp).Select(c => c.id_comp).Take(20).ToListAsync();

        private async Task<Resultado> EmitirAsync(MDFacturacion fe, MDComprobante comp, List<MDComprobante_Detalle> lineas, MDComprobante referencia)
        {
            // Las notas de credito (anulaciones) siempre se permiten; los comprobantes nuevos cuentan para el plan
            if (referencia == null && await _planes.PuedeAgregarAsync(Recurso.Comprobante) is string limite)
            {
                return new Resultado(null, limite);
            }
            using var certificado = Certificado(fe);
            await using (var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable))
            {
                comp.comp_numero = await SiguienteNumeroAsync(comp.comp_tipo, comp.comp_serie);
                var xml = _ubl.Generar(fe, comp, lineas, certificado, referencia);
                comp.comp_xml = xml.Xml;
                comp.comp_hash = xml.Hash;
                _context.Comprobantes.Add(comp);
                await _context.SaveChangesAsync();
                lineas.ForEach(l => l.id_comp = comp.id_comp);
                _context.Comprobante_Detalles.AddRange(lineas);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            // El comprobante ya existe con su numero; si el envio falla se reintenta despues
            return await EnviarAsync(fe, comp);
        }

        private async Task<Resultado> EnviarAsync(MDFacturacion fe, MDComprobante comp)
        {
            string usuario, clave;
            if (fe.fe_ambiente == AmbienteFe.Pruebas && string.IsNullOrWhiteSpace(fe.fe_usuario_sol))
            {
                usuario = SunatClient.UsuarioPruebas;
                clave = SunatClient.ClavePruebas;
            }
            else
            {
                usuario = fe.fe_ruc + fe.fe_usuario_sol?.Trim().ToUpperInvariant();
                clave = Descifrar(fe.fe_clave_sol);
            }
            var url = comp.comp_ambiente == AmbienteFe.Produccion ? SunatClient.UrlProduccion : SunatClient.UrlPruebas;

            comp.comp_intentos++;
            comp.comp_enviado = Fechas.AhoraPeru();
            SunatClient.Respuesta r;
            try
            {
                r = await _sunat.EnviarAsync(url, usuario, clave, UblService.Nombre(fe.fe_ruc, comp), comp.comp_xml);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Respuesta corrupta u otro fallo inesperado: el comprobante queda para reintentar
                _logger.LogError(ex, "Error inesperado al enviar {Numero} a SUNAT", comp.Numero);
                r = new SunatClient.Respuesta(false, false, true, null, "Error inesperado al enviar a SUNAT: " + ex.Message, null);
            }
            comp.comp_sunat_codigo = r.Codigo;
            comp.comp_sunat_mensaje = r.Mensaje?.Length > 1000 ? r.Mensaje[..1000] : r.Mensaje;
            comp.comp_cdr = r.Cdr ?? comp.comp_cdr;
            comp.comp_estado = r.Aceptado ? (r.Observado ? EstadoComprobante.Observado : EstadoComprobante.Aceptado)
                             : r.Reintentable ? EstadoComprobante.ErrorEnvio : EstadoComprobante.Rechazado;
            await _context.SaveChangesAsync();

            if (!r.Aceptado)
            {
                _logger.LogWarning("SUNAT {Estado} {Numero}: {Codigo} {Mensaje}", EstadoComprobante.Nombre(comp.comp_estado), comp.Numero, r.Codigo, r.Mensaje);
            }
            return new Resultado(comp, r.Aceptado ? null : $"{comp.Numero}: {EstadoComprobante.Nombre(comp.comp_estado)} por SUNAT ({r.Codigo}) {r.Mensaje}");
        }

        /// <summary>Correlativo atomico por serie (la transaccion serializable evita numeros repetidos)</summary>
        private async Task<int> SiguienteNumeroAsync(string tipo, string serie)
        {
            var clave = tipo + serie;
            var fila = await _context.Series.FirstOrDefaultAsync(s => s.serie_clave == clave);
            if (fila == null)
            {
                // Si ya hay comprobantes con esa serie (por ejemplo migrados), continua desde el mayor
                var ultimo = await _context.Comprobantes.Where(c => c.comp_tipo == tipo && c.comp_serie == serie).MaxAsync(c => (int?)c.comp_numero) ?? 0;
                fila = new MDSerie { serie_clave = clave, serie_ultimo = ultimo };
                _context.Series.Add(fila);
            }
            fila.serie_ultimo++;
            await _context.SaveChangesAsync();
            return fila.serie_ultimo;
        }

        // ------------------------------------------------------------------ representacion impresa

        /// <summary>Texto del QR segun SUNAT: RUC|TIPO|SERIE|NUMERO|IGV|TOTAL|FECHA|TIPO DOC|NUM DOC|HASH|</summary>
        public static string Qr(string ruc, MDComprobante c) =>
            string.Join("|", ruc, c.comp_tipo, c.comp_serie, c.comp_numero.ToString(),
                c.comp_igv.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                c.comp_total.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                c.comp_fecha.ToString("yyyy-MM-dd"), c.cli_tipo_doc, c.cli_num_doc, c.comp_hash) + "|";
    }

    public static class Documentos
    {
        public static bool DniValido(string dni) => dni?.Trim().Length == 8 && dni.Trim().All(char.IsDigit);

        /// <summary>RUC de 11 digitos con prefijo valido y digito verificador correcto</summary>
        public static bool RucValido(string ruc)
        {
            ruc = ruc?.Trim();
            if (ruc == null || ruc.Length != 11 || !ruc.All(char.IsDigit)) return false;
            if (!new[] { "10", "15", "16", "17", "20" }.Contains(ruc[..2])) return false;
            int[] factores = { 5, 4, 3, 2, 7, 6, 5, 4, 3, 2 };
            var suma = factores.Select((f, i) => f * (ruc[i] - '0')).Sum();
            var dv = 11 - suma % 11;
            if (dv == 10) dv = 0;
            else if (dv == 11) dv = 1;
            return dv == ruc[10] - '0';
        }
    }
}
