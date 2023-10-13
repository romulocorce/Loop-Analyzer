using Loop_Analyzer.banco;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using System.Reflection;

namespace Loop_Analyzer.Banco
{
    public class ConexaoSQLServer 
    {
        public static SqlConnection connOrigemSQLServer = null;
        public static SqlConnection connDestinoSQLServer = null;

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

        public static List<T> RodarSqlBancoOrigem<T>(string sql)
        {
            try
            {
                var ConOrigem = connOrigemSQLServer;
                return ConOrigem.Query<T>(sql, commandTimeout: 200000).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static StatusRetorno incluirBulkInsert<T>(SqlConnection con, SqlTransaction transaction, List<T> dados, string tabela)
        {
            StatusRetorno retorno = new StatusRetorno();
            using (var bulkCopy = new SqlBulkCopy(con, SqlBulkCopyOptions.KeepIdentity, transaction))
            {
                try
                {
                    DataTable table = new DataTable();
                    table = ConvertToDataTable(dados, tabela);
                    if (table != null && table.Columns != null && table.Columns.Count > 0 && !String.IsNullOrEmpty(tabela))
                    {
                        bulkCopy.BatchSize = dados.Count();
                        bulkCopy.DestinationTableName = tabela;
                        bulkCopy.BulkCopyTimeout = 200000;
                        foreach (DataColumn c in table.Columns)
                            bulkCopy.ColumnMappings.Add(c.ColumnName, c.ColumnName);
                        bulkCopy.WriteToServer(table);
                        retorno.Status = 1;
                    }
                    else
                    {
                        retorno.Mensagem = "Erro para carrgar Table";
                        retorno.Status = 0;
                    }
                }
                catch (Exception ex)
                {
                    retorno.Mensagem = ex.Message;
                    retorno.Status = 0;
                }
            }
            return retorno;
        }

        public static DataTable ConvertToDataTable<T>(IEnumerable<T> list, string tableName)
        {
            var table = new DataTable(tableName);
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                       .Where(x => x.CanRead && x.PropertyType.Namespace.Equals("System"))
                                       .ToList();

            if (!props.Any() && typeof(T).Namespace.Equals("System"))
            {
                table.Columns.Add("ID", typeof(T));
                foreach (var item in list)
                {
                    table.Rows.Add(item);
                }

                return table;
            }
            foreach (var x in props)
            {
                var type = Nullable.GetUnderlyingType(x.PropertyType) ?? x.PropertyType;
                table.Columns.Add(x.Name, type);
            }

            var values = new object[props.Count()];
            foreach (var item in list)
            {
                for (var i = 0; i < values.Length; i++)
                    values[i] = props[i].GetValue(item);

                table.Rows.Add(values);
            }

            return table;
        }


    }
}
