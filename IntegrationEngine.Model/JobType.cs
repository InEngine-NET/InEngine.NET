using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Model
{
    public class JobType : IJobType
    {
        public string Name { get; set; }
        public string FullName { get; set; }
    }
}
