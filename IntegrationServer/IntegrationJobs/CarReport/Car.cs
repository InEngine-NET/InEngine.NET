using System;
using IntegrationEngine.Core.Reports;

namespace IntegrationServer
{
    public class Car : IDatum
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }
}

