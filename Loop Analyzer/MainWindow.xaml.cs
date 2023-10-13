using Loop_Analyzer.banco;
using Loop_Analyzer.Banco;
using Loop_Analyzer.business;
using Loop_Analyzer.Telas;
using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.Windows;
using System.Windows.Controls;

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
            hInicio = DateTime.Now;

            StatusRetorno retornoZorro = new StatusRetorno();

            if (lacoFor.IsChecked.GetValueOrDefault(false))
                MessageBox.Show("Opção selecionada: lacoFor");
            else if (lacoForParalelo.IsChecked.GetValueOrDefault(false))
                MessageBox.Show("Opção selecionada: lacoForParalelo");
            else if (LINQ.IsChecked.GetValueOrDefault(false))
                retornoZorro = ConverterCliente("yyyy-MM-dd");

            //MessageBox.Show("Opção selecionada: LINQ");

        }

        public StatusRetorno ConverterCliente(string format)
        {
            StatusRetorno retorno = new StatusRetorno();
            try
            {
                string sql = txtSelect.Text;


                ClienteLinq cb = new ClienteLinq();
                retorno = cb.ConverterCliente(sql, format).Result;


                statusConversao(retorno);

                return retorno;
            }
            catch (Exception ex)
            {
                retorno.Mensagem = ex.Message;
                retorno.Status = 0;
                return retorno;
            }

        }

        private void statusConversao(StatusRetorno retorno)
        {
            hFim = DateTime.Now;
            TimeSpan date = Convert.ToDateTime(hFim) - Convert.ToDateTime(hInicio);

            MessageBox.Show(retorno.Mensagem, "Hora de Inicio: " + Convert.ToString(hInicio)
                + "\n Hora de Termino: " + Convert.ToString(hFim)
                + "\n Tempo Total de Execussão = " + Convert.ToString(date),
                MessageBoxButton.OK, MessageBoxImage.Exclamation);

            if (retorno.Status != 1)
                MessageBox.Show(retorno.Mensagem, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

        }
    }
}

