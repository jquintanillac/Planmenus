using Planmenus.Data.Entities;
using Planmenus.Helpers;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace Planmenus.Services.Facturacion
{
    /// <summary>
    /// Genera y firma el XML UBL 2.1 de boletas, facturas y notas de credito segun las
    /// especificaciones de SUNAT (version 2.1, personalizacion 2.0).
    /// </summary>
    public class UblService
    {
        private const string NsInvoice = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        private const string NsCreditNote = "urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2";
        private const string NsCac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        private const string NsCbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        private const string NsExt = "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2";
        private const string NsDs = "http://www.w3.org/2000/09/xmldsig#";
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public record Resultado(byte[] Xml, string Hash, string NombreArchivo);

        /// <summary>Nombre del archivo segun SUNAT: RUC-TIPO-SERIE-NUMERO</summary>
        public static string Nombre(string ruc, MDComprobante c) => $"{ruc}-{c.comp_tipo}-{c.comp_serie}-{c.comp_numero}";

        public Resultado Generar(MDFacturacion fe, MDComprobante c, List<MDComprobante_Detalle> lineas, X509Certificate2 certificado, MDComprobante referencia = null)
        {
            var esNota = c.comp_tipo == TipoComprobante.NotaCredito;
            var sb = new StringBuilder();
            using (var w = XmlWriter.Create(sb, new XmlWriterSettings { Indent = false, OmitXmlDeclaration = true }))
            {
                w.WriteStartElement(esNota ? "CreditNote" : "Invoice", esNota ? NsCreditNote : NsInvoice);
                w.WriteAttributeString("xmlns", "cac", null, NsCac);
                w.WriteAttributeString("xmlns", "cbc", null, NsCbc);
                w.WriteAttributeString("xmlns", "ext", null, NsExt);
                w.WriteAttributeString("xmlns", "ds", null, NsDs);

                // La firma se inserta despues dentro de ExtensionContent
                w.WriteStartElement("ext", "UBLExtensions", NsExt);
                w.WriteStartElement("ext", "UBLExtension", NsExt);
                w.WriteStartElement("ext", "ExtensionContent", NsExt);
                w.WriteEndElement();
                w.WriteEndElement();
                w.WriteEndElement();

                Cbc(w, "UBLVersionID", "2.1");
                Cbc(w, "CustomizationID", "2.0");
                Cbc(w, "ID", $"{c.comp_serie}-{c.comp_numero}");
                Cbc(w, "IssueDate", c.comp_fecha.ToString("yyyy-MM-dd", Inv));
                Cbc(w, "IssueTime", c.comp_fecha.ToString("HH:mm:ss", Inv));
                if (!esNota)
                {
                    w.WriteStartElement("cbc", "InvoiceTypeCode", NsCbc);
                    w.WriteAttributeString("listID", "0101"); // venta interna
                    w.WriteString(c.comp_tipo);
                    w.WriteEndElement();
                }
                w.WriteStartElement("cbc", "Note", NsCbc);
                w.WriteAttributeString("languageLocaleID", "1000"); // monto en letras
                w.WriteString(NumeroLetras.Soles(c.comp_total));
                w.WriteEndElement();
                Cbc(w, "DocumentCurrencyCode", "PEN");

                if (esNota && referencia != null)
                {
                    w.WriteStartElement("cac", "DiscrepancyResponse", NsCac);
                    Cbc(w, "ReferenceID", $"{referencia.comp_serie}-{referencia.comp_numero}");
                    Cbc(w, "ResponseCode", c.nc_motivo ?? "01");
                    Cbc(w, "Description", c.nc_sustento ?? "ANULACION DE LA OPERACION");
                    w.WriteEndElement();
                    w.WriteStartElement("cac", "BillingReference", NsCac);
                    w.WriteStartElement("cac", "InvoiceDocumentReference", NsCac);
                    Cbc(w, "ID", $"{referencia.comp_serie}-{referencia.comp_numero}");
                    Cbc(w, "DocumentTypeCode", referencia.comp_tipo);
                    w.WriteEndElement();
                    w.WriteEndElement();
                }

                // Referencia a la firma
                w.WriteStartElement("cac", "Signature", NsCac);
                Cbc(w, "ID", "SIGN");
                w.WriteStartElement("cac", "SignatoryParty", NsCac);
                w.WriteStartElement("cac", "PartyIdentification", NsCac);
                Cbc(w, "ID", fe.fe_ruc);
                w.WriteEndElement();
                w.WriteStartElement("cac", "PartyName", NsCac);
                Cbc(w, "Name", fe.fe_razon_social);
                w.WriteEndElement();
                w.WriteEndElement();
                w.WriteStartElement("cac", "DigitalSignatureAttachment", NsCac);
                w.WriteStartElement("cac", "ExternalReference", NsCac);
                Cbc(w, "URI", "#SIGN");
                w.WriteEndElement();
                w.WriteEndElement();
                w.WriteEndElement();

                // Emisor
                w.WriteStartElement("cac", "AccountingSupplierParty", NsCac);
                w.WriteStartElement("cac", "Party", NsCac);
                Identificacion(w, "6", fe.fe_ruc);
                if (!string.IsNullOrWhiteSpace(fe.fe_nombre_comercial))
                {
                    w.WriteStartElement("cac", "PartyName", NsCac);
                    Cbc(w, "Name", fe.fe_nombre_comercial);
                    w.WriteEndElement();
                }
                w.WriteStartElement("cac", "PartyLegalEntity", NsCac);
                Cbc(w, "RegistrationName", fe.fe_razon_social);
                w.WriteStartElement("cac", "RegistrationAddress", NsCac);
                if (!string.IsNullOrWhiteSpace(fe.fe_ubigeo)) Cbc(w, "ID", fe.fe_ubigeo);
                Cbc(w, "AddressTypeCode", string.IsNullOrWhiteSpace(fe.fe_establecimiento) ? "0000" : fe.fe_establecimiento);
                if (!string.IsNullOrWhiteSpace(fe.fe_direccion))
                {
                    w.WriteStartElement("cac", "AddressLine", NsCac);
                    Cbc(w, "Line", fe.fe_direccion);
                    w.WriteEndElement();
                }
                w.WriteStartElement("cac", "Country", NsCac);
                Cbc(w, "IdentificationCode", "PE");
                w.WriteEndElement();
                w.WriteEndElement();
                w.WriteEndElement();
                w.WriteEndElement();
                w.WriteEndElement();

                // Cliente
                w.WriteStartElement("cac", "AccountingCustomerParty", NsCac);
                w.WriteStartElement("cac", "Party", NsCac);
                Identificacion(w, c.cli_tipo_doc, c.cli_num_doc);
                w.WriteStartElement("cac", "PartyLegalEntity", NsCac);
                Cbc(w, "RegistrationName", c.cli_nombre);
                if (!string.IsNullOrWhiteSpace(c.cli_direccion))
                {
                    w.WriteStartElement("cac", "RegistrationAddress", NsCac);
                    w.WriteStartElement("cac", "AddressLine", NsCac);
                    Cbc(w, "Line", c.cli_direccion);
                    w.WriteEndElement();
                    w.WriteEndElement();
                }
                w.WriteEndElement();
                w.WriteEndElement();
                w.WriteEndElement();

                // Forma de pago (obligatoria en facturas)
                if (c.comp_tipo == TipoComprobante.Factura)
                {
                    w.WriteStartElement("cac", "PaymentTerms", NsCac);
                    Cbc(w, "ID", "FormaPago");
                    Cbc(w, "PaymentMeansID", "Contado");
                    w.WriteEndElement();
                }

                Impuesto(w, c.comp_gravada, c.comp_igv, null);

                w.WriteStartElement("cac", esNota ? "LegalMonetaryTotal" : "LegalMonetaryTotal", NsCac);
                Monto(w, "LineExtensionAmount", c.comp_gravada);
                Monto(w, "TaxInclusiveAmount", c.comp_total);
                Monto(w, "PayableAmount", c.comp_total);
                w.WriteEndElement();

                foreach (var l in lineas)
                {
                    w.WriteStartElement("cac", esNota ? "CreditNoteLine" : "InvoiceLine", NsCac);
                    Cbc(w, "ID", l.item.ToString(Inv));
                    w.WriteStartElement("cbc", esNota ? "CreditedQuantity" : "InvoicedQuantity", NsCbc);
                    w.WriteAttributeString("unitCode", "NIU");
                    w.WriteString(l.cantidad.ToString("0.###", Inv));
                    w.WriteEndElement();
                    Monto(w, "LineExtensionAmount", l.valor_venta);
                    w.WriteStartElement("cac", "PricingReference", NsCac);
                    w.WriteStartElement("cac", "AlternativeConditionPrice", NsCac);
                    Monto(w, "PriceAmount", l.precio, "0.00##");
                    Cbc(w, "PriceTypeCode", "01"); // precio unitario con impuestos
                    w.WriteEndElement();
                    w.WriteEndElement();
                    Impuesto(w, l.valor_venta, l.igv, c.comp_tasa_igv);
                    w.WriteStartElement("cac", "Item", NsCac);
                    Cbc(w, "Description", l.descripcion);
                    w.WriteEndElement();
                    w.WriteStartElement("cac", "Price", NsCac);
                    Monto(w, "PriceAmount", l.valor_unitario, "0.00##");
                    w.WriteEndElement();
                    w.WriteEndElement();
                }

                w.WriteEndElement();
            }

            var doc = new XmlDocument { PreserveWhitespace = true };
            doc.LoadXml(sb.ToString());
            var hash = Firmar(doc, certificado);

            using var ms = new MemoryStream();
            using (var xw = XmlWriter.Create(ms, new XmlWriterSettings { Encoding = new UTF8Encoding(false), Indent = false }))
            {
                doc.Save(xw);
            }
            return new Resultado(ms.ToArray(), hash, Nombre(fe.fe_ruc, c));
        }

        /// <summary>
        /// Firma XMLDSig envolvente (RSA-SHA1, C14N) dentro de ext:ExtensionContent. Devuelve el DigestValue.
        /// </summary>
        private static string Firmar(XmlDocument doc, X509Certificate2 certificado)
        {
            using var rsa = certificado.GetRSAPrivateKey() ?? throw new InvalidOperationException("El certificado no tiene clave privada RSA.");
            var firma = new SignedXml(doc) { SigningKey = rsa };
            firma.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
            firma.SignedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;
            var referencia = new Reference("") { DigestMethod = SignedXml.XmlDsigSHA1Url };
            referencia.AddTransform(new XmlDsigEnvelopedSignatureTransform());
            firma.AddReference(referencia);
            var info = new KeyInfo();
            info.AddClause(new KeyInfoX509Data(certificado));
            firma.KeyInfo = info;
            firma.Signature.Id = "SIGN";
            firma.ComputeSignature();

            var nodoFirma = firma.GetXml();
            var ns = new XmlNamespaceManager(doc.NameTable);
            ns.AddNamespace("ext", NsExt);
            var contenido = doc.SelectSingleNode("//ext:UBLExtensions/ext:UBLExtension/ext:ExtensionContent", ns);
            contenido.AppendChild(doc.ImportNode(nodoFirma, true));
            return nodoFirma.GetElementsByTagName("DigestValue")[0].InnerText;
        }

        private static void Cbc(XmlWriter w, string nombre, string valor) => w.WriteElementString("cbc", nombre, NsCbc, valor ?? "");

        private static void Monto(XmlWriter w, string nombre, decimal valor, string formato = "0.00")
        {
            w.WriteStartElement("cbc", nombre, NsCbc);
            w.WriteAttributeString("currencyID", "PEN");
            w.WriteString(valor.ToString(formato, Inv));
            w.WriteEndElement();
        }

        private static void Identificacion(XmlWriter w, string tipo, string numero)
        {
            w.WriteStartElement("cac", "PartyIdentification", NsCac);
            w.WriteStartElement("cbc", "ID", NsCbc);
            w.WriteAttributeString("schemeID", string.IsNullOrWhiteSpace(tipo) ? "0" : tipo);
            w.WriteString(string.IsNullOrWhiteSpace(numero) ? "-" : numero);
            w.WriteEndElement();
            w.WriteEndElement();
        }

        /// <summary>IGV (tributo 1000) del documento o de una linea (con porcentaje y afectacion 10 = gravado)</summary>
        private static void Impuesto(XmlWriter w, decimal baseImponible, decimal igv, decimal? tasa)
        {
            w.WriteStartElement("cac", "TaxTotal", NsCac);
            Monto(w, "TaxAmount", igv);
            w.WriteStartElement("cac", "TaxSubtotal", NsCac);
            Monto(w, "TaxableAmount", baseImponible);
            Monto(w, "TaxAmount", igv);
            w.WriteStartElement("cac", "TaxCategory", NsCac);
            if (tasa != null)
            {
                Cbc(w, "Percent", tasa.Value.ToString("0.00", Inv));
                Cbc(w, "TaxExemptionReasonCode", "10");
            }
            w.WriteStartElement("cac", "TaxScheme", NsCac);
            Cbc(w, "ID", "1000");
            Cbc(w, "Name", "IGV");
            Cbc(w, "TaxTypeCode", "VAT");
            w.WriteEndElement();
            w.WriteEndElement();
            w.WriteEndElement();
            w.WriteEndElement();
        }
    }
}
