using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.IntegrationJob
{
    public class IntegrationJobRunFailureException : Exception
    {
        public IntegrationJobRunFailureException()
        {}

        public IntegrationJobRunFailureException(string message) 
            : base(message)
        {}

        public IntegrationJobRunFailureException(string message, Exception innerException)
            : base(message, innerException)
        {}
    }
}
