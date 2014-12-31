using System;
using IntegrationEngine.Jobs;
using IntegrationEngine.Reports;

namespace IntegrationEngine.ConsoleHost.IntegrationJobs.CarReport
{
    public class CarReportJob : IIntegrationJob
    {
        public TimeSpan Interval { get; set; }
        public DateTimeOffset StartTimeUtc { get; set; }

        public CarReportJob()
        {
            Interval = TimeSpan.FromSeconds(2);
        }

        public void Run()
        {
            // Create a CarReport.
        }
    }
}
