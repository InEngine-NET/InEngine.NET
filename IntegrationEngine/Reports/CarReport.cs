using System;
using System.Collections.Generic;

namespace IntegrationEngine.Reports
{
    public class CarReport : IReport<Car>
    {
        public DateTime Created { get; set; }
        public IList<Car> Data { get; set; }
    }
}
