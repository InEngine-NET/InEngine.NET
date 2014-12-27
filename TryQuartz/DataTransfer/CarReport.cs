using System;
using System.Collections.Generic;

namespace TryQuartz.DataTransfer
{
    public class CarReport
    {
        public DateTime Created { get; set; }
        public List<Car> Data { get; set; }
    }
}
