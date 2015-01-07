using System;
using System.Collections.Generic;

namespace IntegrationEngine.Core.Reports
{
    public interface IReport<T>
    {
        DateTime Created { get; set; }
        IList<T> Data { get; set; }
    }
}
