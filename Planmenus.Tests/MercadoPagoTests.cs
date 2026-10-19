using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Planmenus.Data.Entities;
using Planmenus.Helpers;
using Planmenus.Services;

namespace Planmenus.Tests
{
    /// <summary>
    /// Cobro de la suscripcion con Mercado Pago (simulado): checkout, retorno, notificaciones firmadas,
    /// pagos repetidos, montos que no coinciden y pagos rechazados.
    /// </summary>
    [Collection("App")]
    public class MercadoPagoTests
    {
        private readonly AppFactory _app;
        private MercadoPagoFalso Mp => _app.Services.GetRequiredService<MercadoPagoFalso>();

        public MercadoPagoTests(AppFactory app)
        {
            _app = app;
        }

        private async Task<MDEmpresa> CrearAsync(string marca)
        {
            using var scope = _app.Services.CreateScope();
            var r = await scope.ServiceProvider.GetRequiredService<EmpresaService>().CrearAsync(
                new EmpresaService.Alta(marca + " Pagos", marca.ToLowerInvariant(), null, marca.ToLowerInvariant() + "-dueno", "Dueño", "ClaveSegura123"));
            Assert.True(r.Ok, r.Error);
            return r.Empresa;
        }

        private Task<MDEmpresa> EmpresaAsync(int id) => _app.Con(db => db.Empresas.AsNoTracking().SingleAsync(e => e.id_empresa == id));
        private Task<List<MDPago_Suscripcion>> PagosAsync(int id) => _app.Con(db => db.Pagos_Suscripcion.AsNoTracking().ToListAsync(), id);

        /// <summary>El cliente elige plan y meses; devuelve la referencia que se envio a Mercado Pago</summary>
        private async Task<string> IniciarPagoAsync(Cliente cliente, int empresa, string plan, int meses)
        {
            var destino = await cliente.PostOkAsync("/Suscripcion", "/Suscripcion/Pagar", ("plan", plan), ("meses", meses));
            Assert.StartsWith("https://www.mercadopago.com.pe/", destino);
            return Mp.Preferencias.Single(p => p.ReferenciaExterna.StartsWith($"pm:{empresa}:{plan}:{meses}:") && destino.EndsWith(p.ReferenciaExterna)).ReferenciaExterna;
        }

        private Task<HttpResponseMessage> NotificarAsync(string idPago, string clave = MercadoPagoFalso.ClaveWebhook)
        {
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var requestId = Guid.NewGuid().ToString();
            var firma = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(clave),
                Encoding.UTF8.GetBytes($"id:{idPago};request-id:{requestId};ts:{ts};"))).ToLowerInvariant();
            var req = new HttpRequestMessage(HttpMethod.Post, $"/MercadoPago/Webhook?data.id={idPago}&type=payment")
            {
                Content = new StringContent($"{{\"action\":\"payment.updated\",\"type\":\"payment\",\"data\":{{\"id\":\"{idPago}\"}}}}", Encoding.UTF8, "application/json")
            };
            req.Headers.Add("x-signature", $"ts={ts},v1={firma}");
            req.Headers.Add("x-request-id", requestId);
            return _app.Anonimo().Http.SendAsync(req);
        }

        [Fact]
        public async Task Pago_aprobado_al_volver_renueva_la_suscripcion_una_sola_vez()
        {
            var marca = Marca.Nueva();
            var b = await CrearAsync(marca);
            try
            {
                var cliente = _app.ClienteDe(b.id_empresa);
                var html = WebUtility.HtmlDecode(await cliente.HtmlAsync("/Suscripcion"));
                Assert.Contains("con Mercado Pago", html);

                var referencia = await IniciarPagoAsync(cliente, b.id_empresa, "estandar", 3);
                Assert.Equal(267m, Mp.Preferencias.Single(p => p.ReferenciaExterna == referencia).Monto);

                var id = Mp.Pagar(referencia, 267m);
                var r = await cliente.Http.GetAsync($"/Suscripcion/Retorno?collection_id={id}&collection_status=approved&payment_id={id}&status=approved&external_reference={Uri.EscapeDataString(referencia)}");
                Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);

                // La prueba gratis (14 dias) no se pierde: los 3 meses se suman a su vencimiento
                var e = await EmpresaAsync(b.id_empresa);
                Assert.Equal("estandar", e.emp_plan);
                Assert.Equal(Fechas.HoyPeru().AddDays(14).AddMonths(3), e.emp_vence);
                var pago = Assert.Single(await PagosAsync(b.id_empresa));
                Assert.Equal(id, pago.pago_mp_id);
                Assert.Equal("Mercado Pago", pago.pago_medio);

                // Volver otra vez o recibir la notificacion no lo aplica de nuevo
                await cliente.Http.GetAsync($"/Suscripcion/Retorno?payment_id={id}&status=approved");
                Assert.Equal(HttpStatusCode.OK, (await NotificarAsync(id)).StatusCode);
                Assert.Single(await PagosAsync(b.id_empresa));
                Assert.Equal(Fechas.HoyPeru().AddDays(14).AddMonths(3), (await EmpresaAsync(b.id_empresa)).emp_vence);

                // Meses que no se ofrecen
                await cliente.PostOkAsync("/Suscripcion", "/Suscripcion/Pagar", ("plan", "estandar"), ("meses", 2));
                Assert.DoesNotContain(Mp.Preferencias, p => p.ReferenciaExterna.StartsWith($"pm:{b.id_empresa}:estandar:2:"));
            }
            finally
            {
                await AislamientoTests.EliminarEmpresaAsync(_app, b.id_empresa);
            }
        }

        [Fact]
        public async Task Notificacion_firmada_aplica_y_rechaza_lo_que_no_corresponde()
        {
            var marca = Marca.Nueva();
            var b = await CrearAsync(marca);
            try
            {
                var cliente = _app.ClienteDe(b.id_empresa);
                var referencia = await IniciarPagoAsync(cliente, b.id_empresa, "premium", 1);

                // Firma falsa: se rechaza sin consultar el pago
                var bueno = Mp.Pagar(referencia, 149m);
                Assert.Equal(HttpStatusCode.Unauthorized, (await NotificarAsync(bueno, "otra-clave")).StatusCode);
                Assert.Empty(await PagosAsync(b.id_empresa));

                // Pagos que no se aplican: monto menor, rechazado, pendiente u otra moneda
                foreach (var id in new[] { Mp.Pagar(referencia, 10m), Mp.Pagar(referencia, 149m, "rejected"), Mp.Pagar(referencia, 149m, "pending"), Mp.Pagar(referencia, 149m, moneda: "USD") })
                {
                    Assert.Equal(HttpStatusCode.OK, (await NotificarAsync(id)).StatusCode);
                }
                Assert.Empty(await PagosAsync(b.id_empresa));
                Assert.Equal("basico", (await EmpresaAsync(b.id_empresa)).emp_plan);

                // Pago desconocido para Mercado Pago
                Assert.Equal(HttpStatusCode.OK, (await NotificarAsync("123")).StatusCode);

                // El bueno, con firma valida, aunque el cliente nunca vuelva al sistema
                Assert.Equal(HttpStatusCode.OK, (await NotificarAsync(bueno)).StatusCode);
                var e = await EmpresaAsync(b.id_empresa);
                Assert.Equal("premium", e.emp_plan);
                Assert.Equal(Fechas.HoyPeru().AddDays(14).AddMonths(1), e.emp_vence);
                Assert.Equal(bueno, Assert.Single(await PagosAsync(b.id_empresa)).pago_mp_id);
            }
            finally
            {
                await AislamientoTests.EliminarEmpresaAsync(_app, b.id_empresa);
            }
        }

        [Fact]
        public async Task Volver_con_el_pago_de_otra_empresa_no_lo_muestra_como_propio()
        {
            var marca = Marca.Nueva();
            var b = await CrearAsync(marca);
            try
            {
                var referencia = await IniciarPagoAsync(_app.ClienteDe(b.id_empresa), b.id_empresa, "basico", 1);
                var id = Mp.Pagar(referencia, 49m);

                // La empresa 1 vuelve con el pago de B: se aplica a B (es su dinero) pero no se le confirma a la empresa 1
                var r = await _app.Cliente().Http.GetAsync($"/Suscripcion/Retorno?payment_id={id}&status=approved");
                Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
                Assert.Empty(await _app.Con(db => db.Pagos_Suscripcion.Where(p => p.pago_mp_id == id).ToListAsync()));
                Assert.Single(await PagosAsync(b.id_empresa));
                Assert.Null((await EmpresaAsync(1)).emp_vence);
            }
            finally
            {
                await AislamientoTests.EliminarEmpresaAsync(_app, b.id_empresa);
            }
        }
    }
}
