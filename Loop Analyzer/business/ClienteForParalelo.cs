using Loop_Analyzer.banco;
using Loop_Analyzer.Banco;
using Loop_Analyzer.Classes;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Loop_Analyzer.business
{
    internal class ClienteForParalelo
    {
        private SqlConnection conDestino = ConexaoSQLServer.connDestinoSQLServer;
        private List<RecursosDTO> dados = new List<RecursosDTO>();
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private int volumeDados = 0;

        public async Task<StatusRetorno> ConverterCliente(string sql, string format, int tipolaco, string vm, int repeticao)
        {
            var retorno = new StatusRetorno { Status = 0 };

            try
            {
                var novoResult = ConexaoSQLServer.RodarSqlBancoOrigem<SqlClienteDTO>(sql);

                Task task1 = GetSystemMemoryInfo(tipolaco, vm, repeticao);
                Task task2 = CarregaListaClienteParalelo(novoResult, format);

                await Task.WhenAll(task1, task2).ConfigureAwait(false);

                using (SqlTransaction transaction = conDestino.BeginTransaction())
                {
                    retorno = ConexaoSQLServer.incluirBulkInsert(conDestino, transaction, dados, "DADOS");

                    if (retorno.Status == 1)
                    {
                        transaction.Commit();
                        dados.Clear();
                    }
                    else
                    {
                        transaction.Rollback();
                        dados.Clear();
                    }
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

        private async Task CarregaListaClienteParalelo(List<SqlClienteDTO> resul, string format = "yyyy-MM-dd")
        {
            try
            {
                List<ClienteDTO> listCliente = new List<ClienteDTO>();
                FuncoesTratarDados ft = new FuncoesTratarDados();
                volumeDados = resul.Count;

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

                cancellationTokenSource.Cancel();
                listCliente.Clear();
            }
            catch (Exception ex)
            {
                string msg = ex.Message + " \n " + ex.InnerException.Message;
                throw new ArgumentException("Dado Inválido:", msg);
            }
        }

        public async Task GetSystemMemoryInfo(int tipolaco, string vm, int repeticao)
        {
            try
            {
                PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                PerformanceCounter memCounter = new PerformanceCounter("Memory", "Available MBytes");

                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    RecursosDTO re = new RecursosDTO();

                    re.PROCESSADOR = cpuCounter.NextValue(); //COLETA O USO DE CPU NAQUELE EXATO MOMENTO
                    re.MEMORIA = memCounter.NextValue();    //COLETA O USO DE MEMORIA NAQUELE EXATO MOMENTO
                    re.TIPOLACO = tipolaco;
                    re.VOLUMEDADOS = volumeDados;
                    re.NUMEROREPETICAO = repeticao;
                    re.VM = vm;
                    dados.Add(re);
                    await Task.Delay(300).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                string msg = ex.Message + " \n " + ex.InnerException?.Message;
                throw new ArgumentException("Dado Inválido:", msg);
            }
        }
    }
}
