using Loop_Analyzer.banco;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Loop_Analyzer.Banco
{
    class ConexaoSQLServer 
    {
        private static SqlConnection connOrigemSQLServer = null;
        private static SqlConnection connDestinoSQLServer = null;

        public static StatusRetorno ConectaSqlServer(string strConexao, int origemDestino)
        {
            var retorno = new StatusRetorno();

            try
            {
                if (origemDestino == 1)
                {
                    connOrigemSQLServer = new SqlConnection(strConexao);
                    connOrigemSQLServer.Open();

                    if (connOrigemSQLServer.State == ConnectionState.Open)
                    {
                        retorno.Status = 1;
                        return retorno;
                    }
                    else
                    {
                        retorno.Status = 2;
                        return retorno;
                    }

                }
                else
                {
                    connDestinoSQLServer = new SqlConnection(strConexao);
                    connDestinoSQLServer.Open();

                    if (connDestinoSQLServer.State == ConnectionState.Open)
                    {
                        retorno.Status = 1;
                        return retorno;
                    }
                    else
                    {
                        retorno.Status = 2;
                        return retorno;
                    }
                }
                
            }
            catch (Exception ex)
            {
                retorno.Mensagem = ex.Message;
                retorno.Status = 0;
                return retorno;
            }
        }

        public static List<string> ListaBancoSqlServer()
        {
            var retorno = new List<string>();

            try
            {
                string sql = "SELECT DISTINCT name FROM sys.databases WHERE database_id > 4";

                using (var cmd = new SqlCommand(sql, connOrigemSQLServer))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        retorno.Add(reader.GetString(0));
                    }
                }

                return retorno;
            }
            catch (Exception)
            {
                return retorno;
            }
        }

        public static DbDataReader RodarSql(string sql)
        {
            try
            {
                var ConOrigem = connOrigemSQLServer;
                SqlCommand cmd = new SqlCommand(sql, ConOrigem);
                cmd.CommandTimeout = 200000;
                return cmd.ExecuteReader();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
