using System;
using System.Collections.Generic;
using IntegrationEngine.Reports;

namespace IntegrationEngine.ConsoleHost.Reports.Car
{
    public class CarReport : IReport<Car>
    {
        public DateTime Created { get; set; }
        public IList<Car> Data { get; set; }
    }
}
