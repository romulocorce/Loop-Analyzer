﻿using Loop_Analyzer.banco;
using Loop_Analyzer.Banco;
using Loop_Analyzer.business;
using Loop_Analyzer.Classes;
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
    private int volumeDados = 0;

    public async Task<StatusRetorno> ConverterCliente(string sql, string format, int tipolaco, string vm, int repeticao)
    {
        var retorno = new StatusRetorno { Status = 0 };

        try
        {
            //CRIAR UMA CONEXAO COM O BANCO DESTINO
            var novoResult = ConexaoSQLServer.RodarSqlBancoOrigem<SqlClienteDTO>(sql);

            //STARTAR AS TAREFAS DE FORMA ASSINCRONA
            Task task1 = GetSystemMemoryInfo(tipolaco, vm, repeticao);
            Task task2 = CarregaListaCliente(novoResult, format);

            await Task.WhenAll(task1, task2).ConfigureAwait(false);

            using (SqlTransaction transaction = conDestino.BeginTransaction())
            {
                retorno = ConexaoSQLServer.incluirBulkInsert(conDestino, transaction, dados, "DADOS"); //INSERINDO OS DADOS

                if (retorno.Status == 1)
                {
                    transaction.Commit();
                    dados.Clear(); //LIMPAR A LISTA PARA QUE NAO INTERFIRA NA PROXIMA METRICA
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

    public async Task CarregaListaCliente(List<SqlClienteDTO> resul, string format = "yyyy-MM-dd")
    {
        try
        {
            var listCliente = new List<ClienteDTO>();
            FuncoesTratarDados ft = new FuncoesTratarDados();
            volumeDados = resul.Count;

            for (int i = 0; i < resul.Count; i++)
            {
                var dr = resul[i];
                var clienteDTO = new ClienteDTO();

                clienteDTO.CPF = await ft.ArrumaCnpjCpf(dr.CPF, "CPF");
                clienteDTO.RG = await ft.AjustaTamanhoStringT(dr.RG, 20);
                clienteDTO.NOME = await ft.ConverteNome(dr.NOME?.Trim(), dr.SOBRENOME?.Trim(), dr.CODIGOOLD);
                clienteDTO.SOBRENOME = ft.ConverteSobrenome(dr.NOME?.Trim(), dr.SOBRENOME?.Trim()).Result;
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

                listCliente.Add(clienteDTO);
            }

            cancellationTokenSource.Cancel(); // CANCELA A EXECUÇÃO DO LOOP EM GetSystemMemoryInfo
            listCliente.Clear();             //LIMPAR A LISTA PARA QUE NAO INTERFIRA NA PROXIMA METRICA
        }
        catch (Exception ex)
        {
            string msg = ex.Message + " \n " + ex.InnerException?.Message;
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
                await Task.Delay(300).ConfigureAwait(false); //NESCESSARIO UMA DELEY PARA GERAR UM CONSUMO, SENAO VEM TUDO 0
            }
        }
        catch (Exception ex)
        {
            string msg = ex.Message + " \n " + ex.InnerException?.Message;
            throw new ArgumentException("Dado Inválido:", msg);
        }
    }
}
