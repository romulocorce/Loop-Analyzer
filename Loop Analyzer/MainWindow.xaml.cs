using Loop_Analyzer.banco;
using Loop_Analyzer.Banco;
using Loop_Analyzer.business;
using Loop_Analyzer.Telas;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace Loop_Analyzer
{
    public partial class MainWindow : Window
    {
        private string strConexaoOrigem = "";
        private string strConexaoDestino = "";
        DateTime hFim;
        DateTime hInicio;

        public MainWindow()
        {
            InitializeComponent();

            txtSelect.Text =
"SELECT"
+ Environment.NewLine + "P.FirstName AS NOME,"
+ Environment.NewLine +"P.LastName AS SOBRENOME,"
+ Environment.NewLine +"A.AddressLine1 AS ENDERECO,"
+ Environment.NewLine +"'' AS NUMERO,"
+ Environment.NewLine +"'' AS BAIRRO,"
+ Environment.NewLine +"A.City AS CIDADE,"
+ Environment.NewLine +"PRO.Name AS ESTADO,"
+ Environment.NewLine +"'' AS CEP,"
+ Environment.NewLine +"'' AS TELEFONE1,"
+ Environment.NewLine +"'' AS NASCIMENTO,"
+ Environment.NewLine +"'' AS RG,"
+ Environment.NewLine +"'' AS CPF,"
+ Environment.NewLine +"NULL AS SEXO,"
+ Environment.NewLine +"'' AS CADASTRO,"
+ Environment.NewLine +"'' AS OBSERVACAO,"
+ Environment.NewLine +"'' AS COMPLEMENTO,"
+ Environment.NewLine +"EM.EmailAddress AS EMAIL,"
+ Environment.NewLine +"'' AS DATULTALT,"
+ Environment.NewLine +"P.BusinessEntityID AS CODIGOOLD"
+ Environment.NewLine +"--SELECT*"
+ Environment.NewLine +"FROM PERSON.Person P"
+ Environment.NewLine +"LEFT JOIN Person.BusinessEntityAddress E ON E.BusinessEntityID = P.BusinessEntityID"
+ Environment.NewLine +"LEFT JOIN Person.Address A ON A.AddressID = E.AddressID"
+ Environment.NewLine +"LEFT JOIN Person.AddressType T ON T.AddressTypeID = E.AddressTypeID"
+ Environment.NewLine +"LEFT JOIN Person.EmailAddress EM ON EM.BusinessEntityID = P.BusinessEntityID"
+ Environment.NewLine +"cross JOIN Person.StateProvince PRO";
        }

        private void btnConectarOrigem_Click(object sender, RoutedEventArgs e)
        {
            strConexaoOrigem = $"Persist Security Info=False; User ID={txtuserOrigem.Text}; Initial Catalog=MASTER; Password={pwdSenhaOrigem.Password}; Data Source={txtIpOrigem.Text}";
            ConectarSqlServer(strConexaoOrigem, 1);
        }

        private void ConectarSqlServer(string strConexao, int origemDestino)
        {
            try
            {
                var retorno = ConexaoSQLServer.ConectaSqlServer(strConexao, origemDestino);

                if (retorno.Status == 1)
                {
                    CarregarBancoSqlServer(origemDestino);
                }
                else
                {
                    MessageBox.Show("Erro ao conectar ao banco.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar ao banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CarregarBancoSqlServer(int origemDestino)
        {
            try
            {
                List<string> listaBancoServ = ConexaoSQLServer.ListaBancoSqlServer();
                if (origemDestino == 1)
                {
                    foreach (string banco in listaBancoServ)
                    {
                        cbxConectarOrigem.Items.Add(banco);
                    }

                    cbxConectarOrigem.IsDropDownOpen = true;
                }
                else
                {
                    foreach (string banco in listaBancoServ)
                    {
                        cbxConectarDestino.Items.Add(banco);
                    }

                    cbxConectarDestino.IsDropDownOpen = true;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar bancos de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cbxConectarOrigem_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (cbxConectarOrigem.SelectedItem == null)
                    return;

                strConexaoOrigem = $"Persist Security Info=False; User ID={txtuserOrigem.Text}; Initial Catalog={cbxConectarOrigem.SelectedItem}; Password={pwdSenhaOrigem.Password}; Data Source={txtIpOrigem.Text}";
                var retorno = ConexaoSQLServer.ConectaSqlServer(strConexaoOrigem, 1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar ao banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cbxConectarDestino_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (cbxConectarDestino.SelectedItem == null)
                    return;

                strConexaoDestino = $"Persist Security Info=False; User ID={txtUserDestino.Text}; Initial Catalog={cbxConectarDestino.SelectedItem}; Password={pwdSenhaDestino.Password}; Data Source={txtIpDestino.Text}";
                var retorno = ConexaoSQLServer.ConectaSqlServer(strConexaoDestino, 2);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar ao banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnConectarDestino_Click(object sender, RoutedEventArgs e)
        {
            strConexaoDestino = $"Persist Security Info=False; User ID={txtuserOrigem.Text}; Initial Catalog=MASTER; Password={pwdSenhaOrigem.Password}; Data Source={txtIpOrigem.Text}";
            ConectarSqlServer(strConexaoDestino, 2);
        }

        private void btnExecutar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtSelect.Text))
                {
                    MessageBox.Show("Digite uma consulta SQL válida.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                RESULTADOS janela = new RESULTADOS(txtSelect.Text);
                janela.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir a janela de resultados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnConverter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusRetorno retornoZorro = new StatusRetorno();
                int quantidadeProcesso = int.Parse(txtQuantidadeProcesso.Text);

                if (quantidadeProcesso > 0)
                {
                    for (int i = 0; i < quantidadeProcesso; i++)
                    {
                        if (lacoFor.IsChecked.GetValueOrDefault(false))
                            retornoZorro = ConverterCliente("yyyy-MM-dd", 0);
                        else if (lacoForParalelo.IsChecked.GetValueOrDefault(false))
                            retornoZorro = ConverterCliente("yyyy-MM-dd", 1);
                        else if (LINQ.IsChecked.GetValueOrDefault(false))
                            retornoZorro = ConverterCliente("yyyy-MM-dd", 2);
                        else if (While.IsChecked.GetValueOrDefault(false))
                            retornoZorro = ConverterCliente("yyyy-MM-dd", 3);
                    }
                }
                else
                    MessageBox.Show("Valor digitado precisa ser maior que zero.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro para converte o valor {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public StatusRetorno ConverterCliente(string format, int tipoLaco)
        {
            StatusRetorno retorno = new StatusRetorno();
            try
            {
                string sql = txtSelect.Text;

                if (tipoLaco == 0)
                {
                    ClienteFor cb = new ClienteFor();
                    retorno = cb.ConverterCliente(sql, format, tipoLaco).Result;
                }
                else if (tipoLaco == 1)
                {
                    ClienteForParalelo cb = new ClienteForParalelo();
                    retorno = cb.ConverterCliente(sql, format, tipoLaco).Result;
                }
                else if (tipoLaco == 2)
                {
                    ClienteLinq cb = new ClienteLinq();
                    retorno = cb.ConverterCliente(sql, format, tipoLaco).Result;
                }
                else if (tipoLaco == 3)
                {
                    ClienteWhile cb = new ClienteWhile();
                    retorno = cb.ConverterCliente(sql, format, tipoLaco).Result;
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
    }
}

