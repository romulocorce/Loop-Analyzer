using System;

namespace Loop_Analyzer.Classes
{
    internal class ProcessamentoDTO
    {
        public double USOMEDIOCPU { get; set; }
        public long USOMEDIOMEMORIA { get; set; }
        public DateTime? TEMPO { get; set; }
        public int VOLUMEDADOS { get; set; }
        public int NUMEROREPETICAO { get; set; }
        public String VM { get; set; }
    }
}
