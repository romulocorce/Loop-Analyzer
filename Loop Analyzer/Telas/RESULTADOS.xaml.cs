using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Loop_Analyzer.Banco;

namespace Loop_Analyzer.Telas
{
    public partial class RESULTADOS : Window
    {
        public string sql;
        public SqlConnection connDestino;

        public RESULTADOS(string consultaSql) // Você pode passar a consulta SQL como argumento
        {
            InitializeComponent();
            this.sql = consultaSql; // Defina a propriedade sql com a consulta passada
            CarregarDados();
        }

        public void CarregarDados()
        {
            try
            {
                DataTable resultado = new DataTable();
                using (DbDataReader resul = ConexaoSQLServer.RodarSql(sql))
                {
                    resultado.Load(resul);
                }

                if (resultado.Columns.Count > 0)
                {
                    GridView gridView = new GridView();

                    foreach (DataColumn coluna in resultado.Columns)
                    {
                        GridViewColumn gridColuna = new GridViewColumn
                        {
                            Header = coluna.ColumnName,
                            DisplayMemberBinding = new Binding($"[{coluna.ColumnName}]")
                        };

                        gridView.Columns.Add(gridColuna);
                    }

                    dataGrid.View = gridView;
                    dataGrid.ItemsSource = resultado.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
