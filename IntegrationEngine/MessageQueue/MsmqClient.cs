using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.MessageQueue
{
    public class MsmqClient : IMessageQueueClient
    {
        public void Publish<T>(T value)
        {
            throw new NotImplementedException();
        }

        public bool IsServerAvailable()
        {
            throw new NotImplementedException();
        }
    }
}
