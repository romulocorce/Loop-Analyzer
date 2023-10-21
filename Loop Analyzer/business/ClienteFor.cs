using Loop_Analyzer.banco;
using Loop_Analyzer.Banco;
using Loop_Analyzer.Classes;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loop_Analyzer.business
{
    internal class ClienteFor
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

                listaCliente = await CarregaListaCliente(novoResult, format);
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

        private async Task<List<ClienteDTO>> CarregaListaCliente(List<SqlClienteDTO> resul, string format = "yyyy-MM-dd")
        {
            List<ClienteDTO> listCliente = new List<ClienteDTO>();
            List<ProcessamentoDTO> ListaProcesso = new List<ProcessamentoDTO>();
            Process process = new Process();
            try
            {
                int total = resul.Count();
                totalRegistroInicial = total;

                Messenger.Default.Send("Aguarde...Analisando Dados via FOR!");
                var inicio = DateTime.Now;
                FuncoesTratarDados ft = new FuncoesTratarDados();

                //await atualizaStatus(total, ft);
                              


                for (int i = 0; i < resul.Count; i++)
                {
                    var dr = resul[i];
                    var clienteDTO = new ClienteDTO();
                    var processamentoDTO = new ProcessamentoDTO();

                    clienteDTO.CPF = await ft.ArrumaCnpjCpf(dr.CPF, "CPF");
                    clienteDTO.RG = await ft.AjustaTamanhoStringT(dr.RG, 20);
                    clienteDTO.NOME = await ft.ConverteNome(dr.NOME?.Trim(), dr.SOBRENOME?.Trim(), dr.CODIGOOLD);
                    clienteDTO.SOBRENOME = await ft.ConverteSobrenome(dr.NOME?.Trim(), dr.SOBRENOME?.Trim());
                    clienteDTO.ENDERECO = await ft.LimpaAcento(ft.AjustaTamanhoStringT(dr.ENDERECO, 100).Result);
                    clienteDTO.NUMERO = await ft.AjustaTamanhoStringT(dr.NUMERO, 10);
                    clienteDTO.BAIRRO = await ft.LimpaAcento(ft.AjustaTamanhoStringT(dr.BAIRRO, 100).Result);
                    clienteDTO.CEP = await ft.ArrumaCEP(dr.CEP);
                    clienteDTO.CIDADE = await ft.LimpaAcento(ft.AjustaTamanhoStringT(dr.CIDADE, 100).Result);
                    clienteDTO.ESTADO = await ft.AjustaTamanhoStringT(dr.ESTADO, 2);
                    clienteDTO.COMPLEMENTO = await ft.AjustaTamanhoStringT(dr.COMPLEMENTO, 50);
                    clienteDTO.TIPOPESSOA = ft.ArrumaCnpjCpf(dr.CPF, "CPF").Result.Length <= 14 ? 2 : 1;
                    clienteDTO.TELEFONE1 = await ft.AjustaTamanhoStringT(ft.SomenteNumeros(dr.TELEFONE1).Result, 20);
                    clienteDTO.NASCIMENTO = await ft.ValidaERetornaData(dr.NASCIMENTO, format);
                    clienteDTO.SEXO = await ft.validaSexo(dr.SEXO);
                    clienteDTO.EMAIL = await ft.validaEmail(ft.AjustaTamanhoStringT(dr.EMAIL, 50).Result);
                    clienteDTO.DATULTALT = await ft.ValidaERetornaData(dr.DATULTALT, format);
                    clienteDTO.DATULTALT = clienteDTO.DATULTALT.GetValueOrDefault(DateTime.Now);

                    processamentoDTO.memoryUsage = process.PrivateMemorySize64;
                    processamentoDTO.cpuUsage = process.TotalProcessorTime.TotalMilliseconds;


                    listCliente.Add(clienteDTO);
                    ListaProcesso.Add(processamentoDTO);
                }

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

        private async Task atualizaStatus(int numRows, FuncoesTratarDados ft)
        {
            while (numRows != ft.RetornaUltimoCodigoSequencial().Result)
            {
                Messenger.Default.Send("Aguarde...Analisando Dados via LINQ! \n " + ft.RetornaUltimoCodigoSequencial().Result + "/" + numRows);
                await Task.Delay(1000);
            }
        }
    }
}
