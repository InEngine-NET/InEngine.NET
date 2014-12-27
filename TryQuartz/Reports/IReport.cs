using System;
using System.Collections.Generic;

namespace TryQuartz.Reports
{
    public interface IReport<T>
    {
        DateTime Created { get; set; }
        IList<T> Data { get; set; }
    }
}
