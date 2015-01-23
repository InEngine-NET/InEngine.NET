using IntegrationEngine.Core.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Tests
{
    public class IntegrationJobFixture : IIntegrationJob
    {
        public static Type Type { get { return typeof(IntegrationJobFixture); } }
        public static string FullName { get { return Type.FullName; } }
        public void Run()
        {}        
    }
}
