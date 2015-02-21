using IntegrationEngine.Core.Reports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationServer
{
    public class SampleDatum : IDatum
    {
        public string Name { get; set; }
        public int Height { get; set; }
        public int Weight { get; set; }
    }
}
