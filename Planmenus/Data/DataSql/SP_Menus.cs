using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Planmenus.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading.Tasks;

namespace Planmenus.Data.DataSql
{
    public class SP_Menus
    {
        public SqlConnection conex;

        public SP_Menus()
        {
            var config = GetConfiguration();
            conex = new SqlConnection(config.GetSection("ConnectionStrings").GetSection("DefaultConnection").Value);
        }
        public IConfigurationRoot GetConfiguration()
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            return builder.Build();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<VMCaja_Q01>> mReporteQ01(DateTime fecini, DateTime fecfin)
        {
            try
            {
                List<VMCaja_Q01> LstQ01 = new List<VMCaja_Q01>();
                SqlCommand cmd = new SqlCommand("[dbo].[SP_CAJA_Q01]", conex);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FECINI", fecini);
                cmd.Parameters.AddWithValue("@FECFIN", fecfin);
                conex.Open();
                SqlDataReader rdr = await cmd.ExecuteReaderAsync();
                while (rdr.Read())
                {
                    VMCaja_Q01 objq01 = new VMCaja_Q01();
                    objq01.id_tipago = Convert.ToInt32(rdr["id_tipago"]);
                    objq01.tipago_desc = rdr["tipago_desc"].ToString();
                    objq01.cant_tippag = Convert.ToInt32(rdr["cant_tippag"]);
                    objq01.caja_monto = Convert.ToDecimal(rdr["caja_monto"]);
                    objq01.caja_fecha = rdr["caja_fecha"].ToString();
                    LstQ01.Add(objq01);
                }
                conex.Close();
                return LstQ01;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fecini"></param>
        /// <param name="fecfin"></param>
        /// <returns></returns>
        public async Task<List<VMCaja_Q06>> mSPcajas(DateTime fecini, DateTime fecfin)
        {
            try
            {
                List<VMCaja_Q06> LstQ06 = new List<VMCaja_Q06>();
                SqlCommand cmd = new SqlCommand("[dbo].[SP_CAJA_Q06]", conex);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FECINI", fecini);
                cmd.Parameters.AddWithValue("@FECFIN", fecfin);
                conex.Open();
                SqlDataReader rdr = await cmd.ExecuteReaderAsync();
                while (rdr.Read())
                {
                    VMCaja_Q06 objq06 = new VMCaja_Q06();
                    objq06.id_caja = Convert.ToInt32(rdr["id_caja"]);
                    objq06.tipago_desc = rdr["tipago_desc"].ToString();
                    objq06.deliv_desc = rdr["deliv_desc"].ToString();
                    objq06.mesa_desc = rdr["mesa_desc"].ToString();
                    objq06.caja_obs = rdr["caja_obs"].ToString();
                    objq06.caja_monto = rdr["caja_monto"].ToString();
                    objq06.caja_fecha = rdr["caja_fecha"].ToString();
                    objq06.caja_act = Convert.ToBoolean(rdr["caja_act"]);
                    LstQ06.Add(objq06);
                }
                conex.Close();
                return LstQ06;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<VMCaja_Q02>> mReporteQ02(DateTime fecini, DateTime fecfin)
        {
            try
            {
                List<VMCaja_Q02> LstQ02 = new List<VMCaja_Q02>();
                SqlCommand cmd = new SqlCommand("[dbo].[SP_CAJA_Q02]", conex);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FECINI", fecini);
                cmd.Parameters.AddWithValue("@FECFIN", fecfin);
                conex.Open();
                SqlDataReader rdr = await cmd.ExecuteReaderAsync();
                while (rdr.Read())
                {
                    VMCaja_Q02 objq02 = new VMCaja_Q02();
                    objq02.tipago_desc = rdr["tipago_desc"].ToString();
                    objq02.caja_plato = rdr["caja_plato"].ToString();
                    objq02.caja_monto = Convert.ToDecimal(rdr["caja_monto"]);
                    objq02.caja_fecha = rdr["caja_fecha"].ToString();
                    LstQ02.Add(objq02);
                }
                conex.Close();
                return LstQ02;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<VMCaja_Q03>> mReporteQ03(DateTime fecini, DateTime fecfin)
        {
            try
            {
                List<VMCaja_Q03> LstQ03 = new List<VMCaja_Q03>();
                SqlCommand cmd = new SqlCommand("[dbo].[SP_CAJA_Q03]", conex);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FECINI", fecini);
                cmd.Parameters.AddWithValue("@FECFIN", fecfin);
                conex.Open();
                SqlDataReader rdr = await cmd.ExecuteReaderAsync();
                while (rdr.Read())
                {
                    VMCaja_Q03 objq03 = new VMCaja_Q03();
                    objq03.id_menu = Convert.ToInt32(rdr["id_menu"]);
                    objq03.menu_desc = rdr["menu_desc"].ToString();
                    objq03.cant_menu = rdr["cant_menu"].ToString();
                    objq03.caja_monto = Convert.ToDecimal(rdr["caja_monto"]);
                    objq03.caja_fecha = rdr["caja_fecha"].ToString();
                    LstQ03.Add(objq03);
                }
                conex.Close();
                return LstQ03;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<VMCaja_Q04>> mReporteQ04(DateTime fecini, DateTime fecfin)
        {
            try
            {
                List<VMCaja_Q04> LstQ04 = new List<VMCaja_Q04>();
                SqlCommand cmd = new SqlCommand("[dbo].[SP_CAJA_Q04]", conex);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FECINI", fecini);
                cmd.Parameters.AddWithValue("@FECFIN", fecfin);
                conex.Open();
                SqlDataReader rdr = await cmd.ExecuteReaderAsync();
                while (rdr.Read())
                {
                    VMCaja_Q04 objq04 = new VMCaja_Q04();
                    objq04.menu_desc = rdr["menu_desc"].ToString();
                    objq04.caja_ticket = rdr["caja_ticket"].ToString();
                    objq04.caja_monto = Convert.ToDecimal(rdr["caja_monto"]);
                    objq04.caja_fecha = rdr["caja_fecha"].ToString();
                    LstQ04.Add(objq04);
                }
                conex.Close();
                return LstQ04;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<List<VMCaja_Q05>> mReporteQ05(DateTime fecini, DateTime fecfin)
        {
            try
            {
                List<VMCaja_Q05> LstQ05 = new List<VMCaja_Q05>();
                SqlCommand cmd = new SqlCommand("[dbo].[SP_CAJA_Q05]", conex);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FECINI", fecini);
                cmd.Parameters.AddWithValue("@FECFIN", fecfin);
                conex.Open();
                SqlDataReader rdr = await cmd.ExecuteReaderAsync();
                while (rdr.Read())
                {
                    VMCaja_Q05 objq05 = new VMCaja_Q05();
                    objq05.fecha = rdr["fecha"].ToString();
                    objq05.Inicial = Convert.ToDecimal(rdr["Inicial"]);
                    objq05.Ventas = Convert.ToDecimal(rdr["Ventas"]);
                    objq05.Ingresos = Convert.ToDecimal(rdr["Ingresos"]);
                    objq05.Egresos = Convert.ToDecimal(rdr["Egresos"]);
                    objq05.Total = Convert.ToDecimal(rdr["Total"]);
                    LstQ05.Add(objq05);
                }
                conex.Close();
                return LstQ05;
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        /// <summary>
        /// Eliminar detalle de vehiculo procesos
        /// </summary>
        /// <param name="id_menu"></param>
        public void mUpdateEvento(int id_menu)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("[DBO].[SP_EVENTOS_U01]", conex);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_MENU", id_menu);
                conex.Open();
                cmd.ExecuteNonQuery();
                conex.Close();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// Eliminar detalle de vehiculo procesos
        /// </summary>
        /// <param name="id_menu"></param>
        public void mEliminarEvento(int id_menu)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("[DBO].[SP_EVENTOS_D01]", conex);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_MENU", id_menu);
                conex.Open();
                cmd.ExecuteNonQuery();
                conex.Close();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

        /// <summary>
        /// Eliminar detalle de vehiculo procesos
        /// </summary>
        /// <param name="id_menu"></param>
        public void mUpdateCaja(int id_caja)
        {
            try
            {
                SqlCommand cmd = new SqlCommand("[DBO].[SP_CAJA_U01]", conex);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@ID_CAJA", id_caja);
                conex.Open();
                cmd.ExecuteNonQuery();
                conex.Close();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }

    }
}
