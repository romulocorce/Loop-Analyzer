using Loop_Analyzer.banco;
using Loop_Analyzer.Banco;
using Loop_Analyzer.Classes;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Loop_Analyzer.business
{
    internal class ClienteLinq
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
                Task task2 = CarregaListaCliente(novoResult, format);

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

        private async Task CarregaListaCliente(List<SqlClienteDTO> resul, string format = "yyyy-MM-dd")
        {
            try
            {
                FuncoesTratarDados ft = new FuncoesTratarDados();
                List<ClienteDTO> listCliente = new List<ClienteDTO>();
                volumeDados = resul.Count;

                listCliente = resul.Select(dr => new ClienteDTO
                {
                    CPF = ft.ArrumaCnpjCpf(dr.CPF, "CPF").Result,
                    RG = ft.AjustaTamanhoStringT(dr.RG, 20).Result,
                    NOME = ft.ConverteNome(dr.NOME?.Trim(), dr.SOBRENOME?.Trim(), dr.CODIGOOLD).Result,
                    SOBRENOME = ft.ConverteSobrenome(dr.NOME?.Trim(), dr.SOBRENOME?.Trim()).Result,
                    ENDERECO = ft.LimpaAcento(ft.AjustaTamanhoStringT(dr.ENDERECO, 100).Result).Result,
                    NUMERO = ft.AjustaTamanhoStringT(dr.NUMERO, 10).Result,
                    BAIRRO = ft.LimpaAcento(ft.AjustaTamanhoStringT(dr.BAIRRO, 100).Result).Result,
                    CEP = ft.ArrumaCEP(dr.CEP).Result,
                    CIDADE = ft.LimpaAcento(ft.AjustaTamanhoStringT(dr.CIDADE, 100).Result).Result,
                    ESTADO = ft.AjustaTamanhoStringT(dr.ESTADO, 2).Result,
                    COMPLEMENTO = ft.AjustaTamanhoStringT(dr.COMPLEMENTO, 50).Result,
                    TIPOPESSOA = ft.ArrumaCnpjCpf(dr.CPF, "CPF").Result.Length <= 14 ? 2 : 1,
                    TELEFONE1 = ft.AjustaTamanhoStringT(ft.SomenteNumeros(dr.TELEFONE1).Result, 20).Result,
                    NASCIMENTO = ft.ValidaERetornaData(dr.NASCIMENTO, format).Result,
                    SEXO = ft.validaSexo(dr.SEXO).Result,
                    EMAIL = ft.validaEmail(ft.AjustaTamanhoStringT(dr.EMAIL, 50).Result).Result,
                    DATULTALT = ft.ValidaERetornaData(dr.DATULTALT, format).Result.GetValueOrDefault(DateTime.Now)
                }).ToList();

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
