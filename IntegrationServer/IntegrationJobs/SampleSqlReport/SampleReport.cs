using System;
using System.Collections.Generic;
using IntegrationEngine.Core.Reports;

namespace IntegrationServer
{
    public class SampleReport : IReport<SampleDatum>
    {
        public DateTime Created { get; set; }
        public IList<SampleDatum> Data { get; set; }
    }
}
