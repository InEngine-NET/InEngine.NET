using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.MessageQueue
{
    public interface IMessageQueueListener
    {
        public void Listener();
    }
}
