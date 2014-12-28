using System;
using System.Collections.Generic;

namespace IntegrationEngine.Reports
{
    public interface IReport<T>
    {
        DateTime Created { get; set; }
        IList<T> Data { get; set; }
    }
}
