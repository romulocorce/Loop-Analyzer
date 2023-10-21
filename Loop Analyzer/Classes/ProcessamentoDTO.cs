using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loop_Analyzer.Classes
{
    internal class ProcessamentoDTO
    {
        public double cpuUsage { get; set; }
        public long memoryUsage { get; set; }
        public DateTime? inicio { get; set; }
        public DateTime? final { get; set; }
        public DateTime? total { get; set; }
    }
}
