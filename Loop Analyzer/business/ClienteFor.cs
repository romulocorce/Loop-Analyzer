using Loop_Analyzer.banco;
using Loop_Analyzer.Banco;
using Loop_Analyzer.Classes;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Loop_Analyzer.business
{
    internal class ClienteFor
    {
        public List<SqlClienteDTO> resulTeste = new List<SqlClienteDTO>();
        private SqlConnection conDestino = ConexaoSQLServer.connDestinoSQLServer;
        private List<ClienteDTO> listaCliente = new List<ClienteDTO>();
        private int totalRegistroInicial = 0;
        bool ativo = true;

        public async Task<StatusRetorno> ConverterCliente(string sql, string format, int tipolaco)
        {
            var retorno = new StatusRetorno { Status = 0 };

            try
            {
                Messenger.Default.Send("Aguarde...Carregando.");

                var novoResult = ConexaoSQLServer.RodarSqlBancoOrigem<SqlClienteDTO>(sql);

                Task task1 = GetSystemMemoryInfo(tipolaco); 
                Task task2 = CarregaListaCliente(novoResult, format);

                await Task.WhenAll(task1, task2);

                resulTeste = novoResult;
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

        public async Task<List<ClienteDTO>> CarregaListaCliente(List<SqlClienteDTO> resul, string format = "yyyy-MM-dd")
        {
            try
            {
                var listCliente = new List<ClienteDTO>(resul.Count);
                FuncoesTratarDados ft = new FuncoesTratarDados();

                for (int i = 0; i < resul.Count; i++)
                {
                    var dr = resul[i];
                    var clienteDTO = new ClienteDTO();

                    clienteDTO.CPF = await ft.ArrumaCnpjCpf(dr.CPF, "CPF");
                    clienteDTO.RG = await ft.AjustaTamanhoStringT(dr.RG, 20);
                    clienteDTO.NOME = await ft.ConverteNome(dr.NOME?.Trim(), dr.SOBRENOME?.Trim(), dr.CODIGOOLD);
                    clienteDTO.SOBRENOME = await ft.ConverteSobrenome(dr.NOME?.Trim(), dr.SOBRENOME?.Trim());
                    clienteDTO.ENDERECO = await ft.LimpaAcento(await ft.AjustaTamanhoStringT(dr.ENDERECO, 100));
                    clienteDTO.NUMERO = await ft.AjustaTamanhoStringT(dr.NUMERO, 10);
                    clienteDTO.BAIRRO = await ft.LimpaAcento(await ft.AjustaTamanhoStringT(dr.BAIRRO, 100));
                    clienteDTO.CEP = await ft.ArrumaCEP(dr.CEP);
                    clienteDTO.CIDADE = await ft.LimpaAcento(await ft.AjustaTamanhoStringT(dr.CIDADE, 100));
                    clienteDTO.ESTADO = await ft.AjustaTamanhoStringT(dr.ESTADO, 2);
                    clienteDTO.COMPLEMENTO = await ft.AjustaTamanhoStringT(dr.COMPLEMENTO, 50);
                    clienteDTO.TIPOPESSOA = (await ft.ArrumaCnpjCpf(dr.CPF, "CPF")).Length <= 14 ? 2 : 1;
                    clienteDTO.TELEFONE1 = await ft.AjustaTamanhoStringT(ft.SomenteNumeros(dr.TELEFONE1).Result, 20);
                    clienteDTO.NASCIMENTO = await ft.ValidaERetornaData(dr.NASCIMENTO, format);
                    clienteDTO.SEXO = await ft.validaSexo(dr.SEXO);
                    clienteDTO.EMAIL = await ft.validaEmail(await ft.AjustaTamanhoStringT(dr.EMAIL, 50));
                    clienteDTO.DATULTALT = await ft.ValidaERetornaData(dr.DATULTALT, format);
                    clienteDTO.DATULTALT = clienteDTO.DATULTALT.GetValueOrDefault(DateTime.Now);

                    listCliente.Add(clienteDTO);

                }
                ativo = false;
                return listCliente;
            }
            catch (Exception ex)
            {
                string msg = ex.Message + " \n " + ex.InnerException.Message;
                throw new ArgumentException("Dado Inválido:", msg);
            }
        }

        public async Task<List<RecursosDTO>> GetSystemMemoryInfo(int tipolaco)
        {
            try
            {
                PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                PerformanceCounter memCounter = new PerformanceCounter("Memory", "Available MBytes");
                PerformanceCounter memCounter2 = new PerformanceCounter("Memory", "Available MBytes");

                List<RecursosDTO> recursos = new List<RecursosDTO>();

                while (ativo)
                {
                    RecursosDTO re = new RecursosDTO();

                    re.PROCESSADOR = cpuCounter.NextValue();
                    re.MEMORIA = memCounter2.RawValue;
                    re.TIPOLACO = tipolaco;
                    System.Threading.Thread.Sleep(100);
                    recursos.Add(re);

                }
                return recursos;

            }
            catch (Exception ex)
            {
                string msg = ex.Message + " \n " + ex.InnerException.Message;
                throw new ArgumentException("Dado Inválido:", msg);
            }
        }
    }
}



