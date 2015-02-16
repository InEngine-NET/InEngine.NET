using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.Configuration
{
    [AttributeUsage(AttributeTargets.Class)]
    public class IntegrationPointConfigurationAttribute : Attribute
    {
        public string Name { get; set; }

        //public IntegrationPointConfigurationAttribute(string name)
        //{
        //    Name = name;
        //}
    }
}
