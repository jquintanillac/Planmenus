using Microsoft.Data.SqlClient;
using Planmenus.Models;
using System.Data;

namespace Planmenus.Data.DataSql
{
    /// <summary>
    /// Acceso a los procedimientos almacenados (definidos en la migracion StoredProcedures).
    /// Cada llamada abre y cierra su propia conexion.
    /// </summary>
    public class SP_Menus
    {
        private readonly string _connectionString;

        public SP_Menus(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public Task<List<VMCaja_Q01>> mReporteQ01(DateTime fecini, DateTime fecfin) =>
            ConsultarRango("[dbo].[SP_CAJA_Q01]", fecini, fecfin, rdr => new VMCaja_Q01
            {
                id_tipago = Convert.ToInt32(rdr["id_tipago"]),
                tipago_desc = Texto(rdr["tipago_desc"]),
                cant_tippag = Convert.ToInt32(rdr["cant_tippag"]),
                caja_monto = Decimal(rdr["caja_monto"]),
                caja_fecha = Texto(rdr["caja_fecha"])
            });

        public Task<List<VMCaja_Q06>> mSPcajas(DateTime fecini, DateTime fecfin) =>
            ConsultarRango("[dbo].[SP_CAJA_Q06]", fecini, fecfin, rdr => new VMCaja_Q06
            {
                id_caja = Convert.ToInt32(rdr["id_caja"]),
                tipago_desc = Texto(rdr["tipago_desc"]),
                deliv_desc = Texto(rdr["deliv_desc"]),
                mesa_desc = Texto(rdr["mesa_desc"]),
                caja_obs = Texto(rdr["caja_obs"]),
                caja_monto = Texto(rdr["caja_monto"]),
                caja_fecha = Texto(rdr["caja_fecha"]),
                caja_act = Convert.ToBoolean(rdr["caja_act"])
            });

        public Task<List<VMCaja_Q02>> mReporteQ02(DateTime fecini, DateTime fecfin) =>
            ConsultarRango("[dbo].[SP_CAJA_Q02]", fecini, fecfin, rdr => new VMCaja_Q02
            {
                tipago_desc = Texto(rdr["tipago_desc"]),
                caja_plato = Texto(rdr["caja_plato"]),
                caja_monto = Decimal(rdr["caja_monto"]),
                caja_fecha = Texto(rdr["caja_fecha"])
            });

        public Task<List<VMCaja_Q03>> mReporteQ03(DateTime fecini, DateTime fecfin) =>
            ConsultarRango("[dbo].[SP_CAJA_Q03]", fecini, fecfin, rdr => new VMCaja_Q03
            {
                id_menu = Convert.ToInt32(rdr["id_menu"]),
                menu_desc = Texto(rdr["menu_desc"]),
                cant_menu = Texto(rdr["cant_menu"]),
                caja_monto = Decimal(rdr["caja_monto"]),
                caja_fecha = Texto(rdr["caja_fecha"])
            });

        public Task<List<VMCaja_Q04>> mReporteQ04(DateTime fecini, DateTime fecfin) =>
            ConsultarRango("[dbo].[SP_CAJA_Q04]", fecini, fecfin, rdr => new VMCaja_Q04
            {
                menu_desc = Texto(rdr["menu_desc"]),
                caja_ticket = Texto(rdr["caja_ticket"]),
                caja_monto = Decimal(rdr["caja_monto"]),
                caja_fecha = Texto(rdr["caja_fecha"])
            });

        public Task<List<VMCaja_Q05>> mReporteQ05(DateTime fecini, DateTime fecfin) =>
            ConsultarRango("[dbo].[SP_CAJA_Q05]", fecini, fecfin, rdr => new VMCaja_Q05
            {
                fecha = Texto(rdr["fecha"]),
                Inicial = Decimal(rdr["Inicial"]),
                Ventas = Decimal(rdr["Ventas"]),
                Ingresos = Decimal(rdr["Ingresos"]),
                Egresos = Decimal(rdr["Egresos"]),
                Total = Decimal(rdr["Total"])
            });

        /// <summary>
        /// Actualiza el evento del calendario con los datos de su menu
        /// </summary>
        public void mUpdateEvento(int id_menu) => EjecutarPorMenu("[dbo].[SP_EVENTOS_U01]", id_menu);

        /// <summary>
        /// Elimina el evento del calendario de un menu
        /// </summary>
        public void mEliminarEvento(int id_menu) => EjecutarPorMenu("[dbo].[SP_EVENTOS_D01]", id_menu);

        private async Task<List<T>> ConsultarRango<T>(string sp, DateTime fecini, DateTime fecfin, Func<SqlDataReader, T> mapear)
        {
            var lista = new List<T>();
            using var conex = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sp, conex) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@FECINI", SqlDbType.Date).Value = fecini.Date;
            cmd.Parameters.Add("@FECFIN", SqlDbType.Date).Value = fecfin.Date;
            await conex.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                lista.Add(mapear(rdr));
            }
            return lista;
        }

        private void EjecutarPorMenu(string sp, int id_menu)
        {
            using var conex = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand(sp, conex) { CommandType = CommandType.StoredProcedure };
            cmd.Parameters.Add("@ID_MENU", SqlDbType.Int).Value = id_menu;
            conex.Open();
            cmd.ExecuteNonQuery();
        }

        private static string Texto(object valor) => valor == DBNull.Value ? "" : valor.ToString();

        private static decimal Decimal(object valor) => valor == DBNull.Value ? 0 : Convert.ToDecimal(valor);
    }
}
