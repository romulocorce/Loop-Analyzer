using Loop_Analyzer.banco;
using Loop_Analyzer.Banco;
using Loop_Analyzer.Classes;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loop_Analyzer.business
{
    internal class ClienteForParalelo
    {
        private SqlConnection conDestino = ConexaoSQLServer.connDestinoSQLServer;
        private List<ClienteDTO> listaCliente = new List<ClienteDTO>();
        private int totalRegistroInicial = 0;

        public async Task<StatusRetorno> ConverterCliente(string sql, string format)
        {
            var retorno = new StatusRetorno { Status = 0 };

            try
            {
                Messenger.Default.Send("Aguarde...Carregando.");

                var novoResult = ConexaoSQLServer.RodarSqlBancoOrigem<SqlClienteDTO>(sql);

                listaCliente = await CarregaListaClienteParalelo(novoResult, format);
                using (SqlTransaction transaction = conDestino.BeginTransaction())
                {
                    Messenger.Default.Send("Aguarde......Inserindo Dados Destino.");
                    retorno = ConexaoSQLServer.incluirBulkInsert(conDestino, transaction, listaCliente, "CLIENTE");
                    retorno.TotalMigrado = listaCliente.Count;
                    retorno.TotalRegistro = totalRegistroInicial;
                    if (retorno.Status == 1)
                        transaction.Commit();
                    else
                        transaction.Rollback();
                }

                return retorno;
            }
            catch (Exception ex)
            {
                retorno.Mensagem = ex.Message;
                retorno.Status = 0;
                return retorno;
            }
        }

        private async Task<List<ClienteDTO>> CarregaListaClienteParalelo(List<SqlClienteDTO> resul, string format = "yyyy-MM-dd")
        {
            List<ClienteDTO> listCliente = new List<ClienteDTO>();

            try
            {
                int total = resul.Count();
                totalRegistroInicial = total;

                Messenger.Default.Send("Aguarde...Analisando Dados via FOR (Paralelo)!");
                var inicio = DateTime.Now;
                FuncoesTratarDados ft = new FuncoesTratarDados();

                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }; // Define o número de threads paralelas

                Parallel.ForEach(resul, parallelOptions, dr =>
                {
                    var clienteDTO = new ClienteDTO();

                    clienteDTO.CPF = ft.ArrumaCnpjCpf(dr.CPF, "CPF").Result;
                    clienteDTO.RG = ft.AjustaTamanhoStringT(dr.RG, 20).Result;
                    clienteDTO.NOME = ft.ConverteNome(dr.NOME?.Trim(), dr.SOBRENOME?.Trim(), dr.CODIGOOLD).Result;
                    clienteDTO.SOBRENOME = ft.ConverteSobrenome(dr.NOME?.Trim(), dr.SOBRENOME?.Trim()).Result;
                    clienteDTO.ENDERECO = ft.LimpaAcento(ft.AjustaTamanhoStringT(dr.ENDERECO, 100).Result).Result;
                    clienteDTO.NUMERO = ft.AjustaTamanhoStringT(dr.NUMERO, 10).Result;
                    clienteDTO.BAIRRO = ft.LimpaAcento(ft.AjustaTamanhoStringT(dr.BAIRRO, 100).Result).Result;
                    clienteDTO.CEP = ft.ArrumaCEP(dr.CEP).Result;
                    clienteDTO.CIDADE = ft.LimpaAcento(ft.AjustaTamanhoStringT(dr.CIDADE, 100).Result).Result;
                    clienteDTO.ESTADO = ft.AjustaTamanhoStringT(dr.ESTADO, 2).Result;
                    clienteDTO.COMPLEMENTO = ft.AjustaTamanhoStringT(dr.COMPLEMENTO, 50).Result;
                    clienteDTO.TIPOPESSOA = ft.ArrumaCnpjCpf(dr.CPF, "CPF").Result.Length <= 14 ? 2 : 1;
                    clienteDTO.TELEFONE1 = ft.AjustaTamanhoStringT(ft.SomenteNumeros(dr.TELEFONE1).Result, 20).Result;
                    clienteDTO.NASCIMENTO = ft.ValidaERetornaData(dr.NASCIMENTO, format).Result;
                    clienteDTO.SEXO = ft.validaSexo(dr.SEXO).Result;
                    clienteDTO.EMAIL = ft.validaEmail(ft.AjustaTamanhoStringT(dr.EMAIL, 50).Result).Result;
                    clienteDTO.DATULTALT = ft.ValidaERetornaData(dr.DATULTALT, format).Result.GetValueOrDefault(DateTime.Now);

                    listCliente.Add(clienteDTO);
                });

                var final = DateTime.Now;
                var tempo = final - inicio;

                return listCliente;
            }
            catch (Exception ex)
            {
                string msg = ex.Message + " \n " + ex.InnerException.Message;
                throw new ArgumentException("Dado Inválido:", msg);
            }
        }
    }
}
