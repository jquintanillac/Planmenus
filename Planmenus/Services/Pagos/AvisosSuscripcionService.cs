using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Services.Facturacion;

namespace Planmenus.Services.Pagos
{
    /// <summary>
    /// Correos y comprobantes de la suscripcion:
    /// - por cada pago: la plataforma emite su boleta o factura y le envia el recibo al restaurante;
    /// - avisos de vencimiento: 5 dias antes (o al terminar la prueba gratis) y al vencer.
    /// Lo ejecuta SuscripcionWorker cada pocos minutos; cada aviso se envia una sola vez.
    /// </summary>
    public class AvisosSuscripcionService
    {
        public const int DiasAviso = 5;
        private static readonly CultureInfo Peru = new("es-PE");

        private readonly IServiceScopeFactory _scopes;
        private readonly EmpresaService _empresas;
        private readonly SuscripcionOptions _opciones;
        private readonly ILogger<AvisosSuscripcionService> _logger;

        public AvisosSuscripcionService(IServiceScopeFactory scopes, EmpresaService empresas, IOptions<SuscripcionOptions> opciones, ILogger<AvisosSuscripcionService> logger)
        {
            _scopes = scopes;
            _empresas = empresas;
            _opciones = opciones.Value;
            _logger = logger;
        }

        // ------------------------------------------------------------------ pagos

        /// <summary>Emite el comprobante y envia el recibo de los pagos que aun no se procesaron</summary>
        public async Task<int> ProcesarPagosAsync(CancellationToken ct = default)
        {
            List<(int Empresa, int Pago)> pendientes;
            using (var scope = _scopes.CreateScope())
            {
                pendientes = (await scope.ServiceProvider.GetRequiredService<Datacontext>().Pagos_Suscripcion.IgnoreQueryFilters()
                        .Where(p => !p.pago_procesado).OrderBy(p => p.id_pago).Select(p => new { p.id_empresa, p.id_pago }).Take(20).ToListAsync(ct))
                    .Select(p => (p.id_empresa, p.id_pago)).ToList();
            }
            foreach (var (empresa, pago) in pendientes)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await ProcesarPagoAsync(empresa, pago, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Se reintenta en la siguiente vuelta
                    _logger.LogError(ex, "No se pudo procesar el pago de suscripción {Pago} de la empresa {Empresa}", pago, empresa);
                }
            }
            return pendientes.Count;
        }

        private async Task ProcesarPagoAsync(int idEmpresa, int idPago, CancellationToken ct)
        {
            // Datos del restaurante que pago (dentro de su empresa)
            var (pago, empresa, cliente, destinos) = await _empresas.EnEmpresaAsync(idEmpresa, async sp =>
            {
                var db = sp.GetRequiredService<Datacontext>();
                var p = await db.Pagos_Suscripcion.AsNoTracking().FirstAsync(x => x.id_pago == idPago, ct);
                var e = await db.Empresas.AsNoTracking().FirstAsync(x => x.id_empresa == idEmpresa, ct);
                var fe = await db.Facturacion.AsNoTracking().OrderBy(f => f.id_fe).FirstOrDefaultAsync(ct);
                var g = await db.Generales.AsNoTracking().OrderBy(x => x.id_gene).FirstOrDefaultAsync(ct);
                var para = await DestinatariosAsync(db, ct);
                var ruc = Documentos.RucValido(e.emp_ruc) ? e.emp_ruc.Trim() : Documentos.RucValido(fe?.fe_ruc) ? fe.fe_ruc.Trim() : null;
                var c = ruc != null
                    ? new FacturacionService.Cliente(TipoComprobante.Factura, "6", ruc, fe?.fe_razon_social ?? g?.gene_empre ?? e.emp_nombre, fe?.fe_direccion ?? g?.gene_direc, para.FirstOrDefault())
                    : new FacturacionService.Cliente(TipoComprobante.Boleta, null, null, g?.gene_empre ?? e.emp_nombre, g?.gene_direc, para.FirstOrDefault());
                return (p, e, c, para);
            });

            // Comprobante de la plataforma (empresa emisora). Si no factura todavia, se envia solo el recibo.
            string numero = pago.pago_comprobante, nota = null;
            byte[] xml = null;
            if (numero == null && pago.pago_monto > 0 && idEmpresa != _opciones.EmpresaEmisora)
            {
                var plan = _opciones.Buscar(pago.pago_plan);
                var descripcion = $"Suscripción Planmenus plan {plan.Nombre} - {pago.pago_meses} {(pago.pago_meses == 1 ? "mes" : "meses")} (hasta {pago.pago_vence:dd/MM/yyyy})";
                var r = await _empresas.EnEmpresaAsync(_opciones.EmpresaEmisora, sp =>
                    sp.GetRequiredService<FacturacionService>().EmitirServicioAsync(descripcion, pago.pago_monto, cliente));
                if (r.Comprobante?.id_comp > 0)
                {
                    numero = r.Comprobante.Numero;
                    xml = r.Comprobante.comp_xml;
                }
                else
                {
                    nota = "Sin comprobante: " + r.Error;
                }
            }

            string errorCorreo = null;
            if (destinos.Count > 0)
            {
                var adjuntos = xml == null ? null : new[] { ($"{numero}.xml", xml, "application/xml") };
                errorCorreo = await _empresas.EnEmpresaAsync(idEmpresa, sp => sp.GetRequiredService<CorreoService>()
                    .EnviarAsync("susc-recibo", destinos, $"Recibo de su pago · {empresa.emp_nombre}", HtmlRecibo(empresa, pago, numero), ct: ct, adjuntos: adjuntos));
            }
            else
            {
                errorCorreo = "El restaurante no tiene administradores con correo.";
            }
            if (errorCorreo != null) nota = string.Join(" · ", new[] { nota, "Recibo no enviado: " + errorCorreo }.Where(x => x != null));
            if (nota?.Length > 300) nota = nota[..300];

            await _empresas.EnEmpresaAsync(idEmpresa, sp => sp.GetRequiredService<Datacontext>().Pagos_Suscripcion.Where(x => x.id_pago == idPago)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.pago_procesado, true)
                                          .SetProperty(x => x.pago_comprobante, numero)
                                          .SetProperty(x => x.pago_nota, nota), ct));
        }

        // ------------------------------------------------------------------ vencimiento

        /// <summary>Envia los avisos de vencimiento que correspondan hoy. Devuelve cuantos envio.</summary>
        public async Task<int> AvisarVencimientosAsync(CancellationToken ct = default)
        {
            List<MDEmpresa> empresas;
            using (var scope = _scopes.CreateScope())
            {
                var hasta = Fechas.HoyPeru().AddDays(DiasAviso);
                var desde = Fechas.HoyPeru().AddDays(-_opciones.DiasGracia);
                empresas = await scope.ServiceProvider.GetRequiredService<Datacontext>().Empresas.AsNoTracking()
                    .Where(e => e.emp_activa && e.emp_vence != null && e.emp_vence <= hasta && e.emp_vence >= desde)
                    .ToListAsync(ct);
            }
            int enviados = 0;
            foreach (var e in empresas)
            {
                ct.ThrowIfCancellationRequested();
                var dias = PlanService.DiasParaVencer(e.emp_vence).Value;
                // El tipo lleva la fecha de vencimiento: al renovar, los avisos del nuevo periodo vuelven a enviarse
                var tipo = (dias > 0 ? "susc-aviso-" : "susc-vencida-") + e.emp_vence.Value.ToString("yyyyMMdd");
                try
                {
                    enviados += await _empresas.EnEmpresaAsync(e.id_empresa, async sp =>
                    {
                        var db = sp.GetRequiredService<Datacontext>();
                        var correo = sp.GetRequiredService<CorreoService>();
                        if (!correo.Opciones.Configurado || await db.Correos.AnyAsync(c => c.cor_tipo == tipo, ct)) return 0;
                        var destinos = await DestinatariosAsync(db, ct);
                        if (destinos.Count == 0) return 0;
                        var prueba = !await db.Pagos_Suscripcion.AnyAsync(ct);
                        var (asunto, html) = HtmlVencimiento(e, dias, prueba);
                        await correo.EnviarAsync(tipo, destinos, asunto, html, ct: ct);
                        return 1;
                    });
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "No se pudo enviar el aviso de vencimiento a la empresa {Empresa}", e.id_empresa);
                }
            }
            return enviados;
        }

        // ------------------------------------------------------------------ ayudas

        /// <summary>Correos de los usuarios activos que administran el restaurante (permiso de Configuracion)</summary>
        private static async Task<List<string>> DestinatariosAsync(Datacontext db, CancellationToken ct)
        {
            var correos = await (from u in db.Usuarios
                                 join ru in db.Rol_Usuarios on u.IdUsuario equals ru.IdUsuario
                                 join r in db.Rols on ru.id_rol equals r.id_rol
                                 join p in db.Rol_Permisos on r.id_rol equals p.id_rol
                                 where u.Usua_act && r.rol_act && p.rolper_act && p.id_submenu == 1 && u.Usua_email != null && u.Usua_email != ""
                                 select u.Usua_email).Distinct().ToListAsync(ct);
            return Correos.Normalizar(correos);
        }

        private string Url(string ruta) => string.IsNullOrWhiteSpace(_opciones.UrlSistema) ? null : _opciones.UrlSistema.TrimEnd('/') + ruta;

        private static string H(string s) => WebUtility.HtmlEncode(s ?? "");

        private string Plantilla(string titulo, string cuerpo, string boton = null, string enlace = null) =>
            $@"<div style=""font-family:Arial,sans-serif;max-width:560px;margin:auto;color:#1f1a17"">
<h2 style=""margin:0 0 14px"">{H(titulo)}</h2>
{cuerpo}
{(enlace == null ? "" : $@"<p style=""margin:24px 0""><a href=""{H(enlace)}"" style=""background:#e4572e;color:#fff;padding:12px 20px;border-radius:10px;text-decoration:none;font-weight:bold"">{H(boton)}</a></p>")}
<p style=""color:#7a6e64;font-size:12px;margin-top:28px"">Planmenus · Este correo se envía a los administradores de su restaurante.</p>
</div>";

        private string HtmlRecibo(MDEmpresa e, MDPago_Suscripcion p, string comprobante)
        {
            var plan = _opciones.Buscar(p.pago_plan);
            var filas = new List<(string, string)>
            {
                ("Restaurante", e.emp_nombre),
                ("Plan", $"{plan.Nombre} · {p.pago_meses} {(p.pago_meses == 1 ? "mes" : "meses")}"),
                ("Monto", "S/ " + p.pago_monto.ToString("N2", Peru)),
                ("Medio de pago", p.pago_medio),
                ("Fecha", p.pago_fecha.ToString("dd/MM/yyyy HH:mm")),
                ("Vigente hasta", p.pago_vence.ToString("dd/MM/yyyy")),
            };
            if (!string.IsNullOrWhiteSpace(p.pago_referencia)) filas.Add(("Referencia", p.pago_referencia));
            if (comprobante != null) filas.Add(("Comprobante electrónico", comprobante));
            var tabla = string.Concat(filas.Select(f => $@"<tr><td style=""padding:6px 12px 6px 0;color:#7a6e64"">{H(f.Item1)}</td><td style=""padding:6px 0""><strong>{H(f.Item2)}</strong></td></tr>"));
            var cuerpo = $@"<p>¡Gracias! Recibimos su pago y su suscripción quedó renovada.</p>
<table style=""border-collapse:collapse;font-size:14px"">{tabla}</table>
{(comprobante != null ? @"<p style=""font-size:13px;color:#7a6e64"">Adjuntamos el XML de su comprobante electrónico.</p>" : "")}";
            return Plantilla("Recibo de su pago", cuerpo, "Ver mi suscripción", Url("/Suscripcion"));
        }

        private (string Asunto, string Html) HtmlVencimiento(MDEmpresa e, int dias, bool prueba)
        {
            var que = prueba ? "Su prueba gratis" : "Su suscripción";
            var fecha = e.emp_vence.Value.ToString("dddd d 'de' MMMM", Peru);
            string asunto, texto;
            if (dias > 0)
            {
                asunto = $"{que} termina en {dias} {(dias == 1 ? "día" : "días")} · {e.emp_nombre}";
                texto = $"<p>{que} de <strong>{H(e.emp_nombre)}</strong> termina el <strong>{H(fecha)}</strong>.</p><p>Renueve desde <em>Administración → Mi suscripción</em> para seguir tomando pedidos sin interrupciones. Los días que pague se suman a los que le quedan.</p>";
            }
            else
            {
                var gracia = _opciones.DiasGracia + dias;
                asunto = $"{que} venció · {e.emp_nombre}";
                texto = $"<p>{que} de <strong>{H(e.emp_nombre)}</strong> venció el <strong>{H(fecha)}</strong>.</p>" +
                        (gracia > 0
                            ? $"<p>Puede seguir trabajando {gracia} {(gracia == 1 ? "día" : "días")} más. Después, el sistema solo mostrará <em>Mi suscripción</em> hasta que renueve.</p>"
                            : "<p>Renueve hoy para seguir usando el sistema.</p>") +
                        "<p>Sus datos están guardados: al renovar, todo sigue como lo dejó.</p>";
            }
            return (asunto, Plantilla(asunto, texto, "Renovar ahora", Url("/Suscripcion")));
        }
    }

    /// <summary>Cada 2 minutos procesa los pagos nuevos; los avisos de vencimiento se revisan cada hora</summary>
    public class SuscripcionWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<SuscripcionWorker> _logger;

        public SuscripcionWorker(IServiceScopeFactory scopes, ILogger<SuscripcionWorker> logger)
        {
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            var ultimosAvisos = DateTime.MinValue;
            using var reloj = new PeriodicTimer(TimeSpan.FromMinutes(2));
            do
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var avisos = scope.ServiceProvider.GetRequiredService<AvisosSuscripcionService>();
                    await avisos.ProcesarPagosAsync(ct);
                    if (DateTime.UtcNow - ultimosAvisos >= TimeSpan.FromHours(1))
                    {
                        await avisos.AvisarVencimientosAsync(ct);
                        ultimosAvisos = DateTime.UtcNow;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error en los avisos de suscripción");
                }
            } while (await reloj.WaitForNextTickAsync(ct));
        }
    }
}
