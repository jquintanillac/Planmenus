using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Planmenus.Data;
using Planmenus.Data.Entities;
using Planmenus.Helpers;

namespace Planmenus.Services
{
    /// <summary>Un plan de suscripcion. 0 en un limite significa "sin limite".</summary>
    public class Plan
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public decimal PrecioMes { get; set; }
        public int Usuarios { get; set; }
        public int Mesas { get; set; }
        public int ComprobantesMes { get; set; }
        public string Descripcion { get; set; }
    }

    /// <summary>Seccion "Suscripcion" de appsettings: planes, dias de prueba y como pagar</summary>
    public class SuscripcionOptions
    {
        public int DiasPrueba { get; set; } = 14;
        /// <summary>Dias despues del vencimiento en que todavia se puede trabajar (con aviso)</summary>
        public int DiasGracia { get; set; } = 3;
        public string InstruccionesPago { get; set; }
        /// <summary>Empresa de la plataforma que emite la boleta o factura de cada suscripcion pagada</summary>
        public int EmpresaEmisora { get; set; } = 1;
        /// <summary>Direccion publica del sistema para los enlaces de los correos (ej. https://app.planmenus.pe)</summary>
        public string UrlSistema { get; set; }
        public List<Plan> Planes { get; set; } = new();

        /// <summary>Planes por defecto si appsettings no define ninguno</summary>
        public static readonly List<Plan> PorDefecto = new()
        {
            new() { Codigo = "basico", Nombre = "Básico", PrecioMes = 49, Usuarios = 3, Mesas = 15, ComprobantesMes = 300, Descripcion = "Para un local pequeño o un puesto de menú." },
            new() { Codigo = "estandar", Nombre = "Estándar", PrecioMes = 89, Usuarios = 8, Mesas = 40, ComprobantesMes = 1500, Descripcion = "Restaurante con salón, mozos y delivery." },
            new() { Codigo = "premium", Nombre = "Premium", PrecioMes = 149, Usuarios = 0, Mesas = 0, ComprobantesMes = 0, Descripcion = "Sin límites, para locales de alto movimiento." },
        };

        public List<Plan> Todos => Planes is { Count: > 0 } ? Planes : PorDefecto;
        public Plan Buscar(string codigo) => Todos.FirstOrDefault(p => p.Codigo == codigo) ?? Todos[0];
    }

    public enum Recurso { Usuario, Mesa, Comprobante }

    /// <summary>
    /// Limites del plan de la empresa actual y estado de su suscripcion.
    /// </summary>
    public class PlanService
    {
        private readonly Datacontext _context;
        private readonly EmpresaActual _empresa;
        private readonly SuscripcionOptions _opciones;

        public PlanService(Datacontext context, EmpresaActual empresa, IOptions<SuscripcionOptions> opciones)
        {
            _context = context;
            _empresa = empresa;
            _opciones = opciones.Value;
        }

        public SuscripcionOptions Opciones => _opciones;

        public record Uso(int Usado, int Limite)
        {
            public bool SinLimite => Limite <= 0;
            public bool Lleno => !SinLimite && Usado >= Limite;
            public int Porcentaje => SinLimite ? 0 : Math.Min(100, Usado * 100 / Math.Max(1, Limite));
        }

        public record Estado(MDEmpresa Empresa, Plan Plan, Uso Usuarios, Uso Mesas, Uso Comprobantes);

        public async Task<Estado> EstadoAsync()
        {
            var empresa = await _context.Empresas.AsNoTracking().FirstAsync(e => e.id_empresa == _empresa.Id);
            var plan = _opciones.Buscar(empresa.emp_plan);
            return new Estado(empresa, plan,
                new Uso(await ContarAsync(Recurso.Usuario), plan.Usuarios),
                new Uso(await ContarAsync(Recurso.Mesa), plan.Mesas),
                new Uso(await ContarAsync(Recurso.Comprobante), plan.ComprobantesMes));
        }

        /// <summary>Cuantos mas se pueden agregar (int.MaxValue si no hay limite)</summary>
        public async Task<int> DisponiblesAsync(Recurso recurso)
        {
            var limite = Limite(_opciones.Buscar(await PlanActualAsync()), recurso);
            if (limite <= 0) return int.MaxValue;
            return Math.Max(0, limite - await ContarAsync(recurso));
        }

        /// <summary>null si se puede agregar uno mas; si no, el mensaje para el usuario</summary>
        public async Task<string> PuedeAgregarAsync(Recurso recurso)
        {
            if (await DisponiblesAsync(recurso) > 0) return null;
            var plan = _opciones.Buscar(await PlanActualAsync());
            var que = recurso switch
            {
                Recurso.Usuario => $"{plan.Usuarios} usuarios activos",
                Recurso.Mesa => $"{plan.Mesas} mesas activas",
                _ => $"{plan.ComprobantesMes} comprobantes electrónicos al mes"
            };
            return $"Su plan {plan.Nombre} permite hasta {que}. Mejore su plan en Mi suscripción para agregar más.";
        }

        private Task<string> PlanActualAsync() =>
            _context.Empresas.Where(e => e.id_empresa == _empresa.Id).Select(e => e.emp_plan).FirstAsync();

        private static int Limite(Plan p, Recurso r) => r switch
        {
            Recurso.Usuario => p.Usuarios,
            Recurso.Mesa => p.Mesas,
            _ => p.ComprobantesMes
        };

        private Task<int> ContarAsync(Recurso r)
        {
            var hoy = Fechas.HoyPeru();
            var mes = new DateTime(hoy.Year, hoy.Month, 1);
            return r switch
            {
                Recurso.Usuario => _context.Usuarios.CountAsync(u => u.Usua_act),
                Recurso.Mesa => _context.Mesas.CountAsync(m => m.mesa_act),
                _ => _context.Comprobantes.CountAsync(c => c.comp_fecha >= mes)
            };
        }

        /// <summary>Dias que faltan para vencer (negativo si ya vencio); null si no vence</summary>
        public static int? DiasParaVencer(DateTime? vence) => vence == null ? null : (int)(vence.Value.Date - Fechas.HoyPeru()).TotalDays;

        /// <summary>Vencida y pasados los dias de gracia: solo puede entrar a Mi suscripcion</summary>
        public static bool Bloqueada(DateTime? vence, int diasGracia) => DiasParaVencer(vence) < -diasGracia;
    }
}
