using Loop_Analyzer.banco;
using Loop_Analyzer.Banco;
using Loop_Analyzer.Classes;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Loop_Analyzer.business
{
    internal class ClienteLinq
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

            try
            {
                int total = resul.Count();
                totalRegistroInicial = total;

                Messenger.Default.Send("Aguarde...Analisando Dados via LINQ!");
                var inicio = DateTime.Now;
                FuncoesTratarDados ft = new FuncoesTratarDados();

                //await atualizaStatus(total, ft);

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
