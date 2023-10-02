namespace Loop_Analyzer.banco
{
    public class StatusRetorno
    {

        // <summary>
        // 0 - Erro na Inclusão, 1 - Inclusão Ok, 2 - Já existe um registro no BD, 3 - Erro de Foreign Key dependence
        // </summary>     
        public int Status;

        public string Mensagem;

        public int TotalRegistro;

        public int TotalMigrado;

        public int TotalErro;

        public int operadorBaixaEBaixaloja;

        public int TotalMenorZero;

       // public List<TABELATOTALDTO> listaTotalTabelas;

        public bool gravouLog;

    }
}

