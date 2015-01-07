using System;
using System.Collections.Generic;
using IntegrationEngine.Core.Reports;

namespace IntegrationEngine.ConsoleHost.IntegrationJobs.SampleSqlReport
{
    public class SampleReport : IReport<SampleDatum>
    {
        public DateTime Created { get; set; }
        public IList<SampleDatum> Data { get; set; }
    }
}
