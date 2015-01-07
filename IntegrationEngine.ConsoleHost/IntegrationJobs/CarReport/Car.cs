using System;
using IntegrationEngine.Core.Reports;

namespace IntegrationEngine.ConsoleHost.IntegrationJobs.CarReport
{
    public class Car : IDatum
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}

