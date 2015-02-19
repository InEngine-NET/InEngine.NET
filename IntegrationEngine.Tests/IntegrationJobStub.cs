using IntegrationEngine.Core.IntegrationJob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Tests
{
    public class IntegrationJobStub : IIntegrationJob
    {
        public static Type Type { get { return typeof(IntegrationJobStub); } }
        public static string FullName { get { return Type.FullName; } }
        public void Run()
        {}        
    }
}
