using System;
using System.Collections.Generic;
using IntegrationEngine.Core.Reports;

namespace IntegrationServer
{
    public class CarReport : IReport<Car>
    {
        public DateTime Created { get; set; }
        public IList<Car> Data { get; set; }
    }
}
