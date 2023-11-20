using Loop_Analyzer.banco;
using Loop_Analyzer.Banco;
using Loop_Analyzer.business;
using Loop_Analyzer.Classes;
using Loop_Analyzer;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System;

internal class ClienteFor
{
    private SqlConnection conDestino = ConexaoSQLServer.connDestinoSQLServer;
    private List<RecursosDTO> dados = new List<RecursosDTO>();
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    public async Task<StatusRetorno> ConverterCliente(string sql, string format, int tipolaco)
    {
        var retorno = new StatusRetorno { Status = 0 };

        try
        {
            Messenger.Default.Send("Aguarde...Carregando.");

            var novoResult = ConexaoSQLServer.RodarSqlBancoOrigem<SqlClienteDTO>(sql);

            Task task1 = GetSystemMemoryInfo(tipolaco);
            Task task2 = CarregaListaCliente(novoResult, format);

            await Task.WhenAll(task1, task2).ConfigureAwait(false);

            using (SqlTransaction transaction = conDestino.BeginTransaction())
            {
                retorno = ConexaoSQLServer.incluirBulkInsert(conDestino, transaction, dados, "DADOS");

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

    public async Task CarregaListaCliente(List<SqlClienteDTO> resul, string format = "yyyy-MM-dd")
    {
        try
        {
            var listCliente = new List<ClienteDTO>();
            FuncoesTratarDados ft = new FuncoesTratarDados();

            for (int i = 0; i < resul.Count; i++)
            {
                var dr = resul[i];
                var clienteDTO = new ClienteDTO();

                clienteDTO.CPF = await ft.ArrumaCnpjCpf(dr.CPF, "CPF").ConfigureAwait(false);
                clienteDTO.RG = await ft.AjustaTamanhoStringT(dr.RG, 20).ConfigureAwait(false);
                // ... (Restante do código)

                listCliente.Add(clienteDTO);
            }

            cancellationTokenSource.Cancel(); // Cancelar a execução do loop em GetSystemMemoryInfo

        }
        catch (Exception ex)
        {
            string msg = ex.Message + " \n " + ex.InnerException?.Message;
            throw new ArgumentException("Dado Inválido:", msg);
        }
    }

    public async Task GetSystemMemoryInfo(int tipolaco)
    {
        try
        {
            PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            PerformanceCounter memCounter = new PerformanceCounter("Memory", "Available MBytes");

            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                RecursosDTO re = new RecursosDTO();

                re.PROCESSADOR = cpuCounter.NextValue();
                re.MEMORIA = memCounter.NextValue();
                re.TIPOLACO = tipolaco;
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
