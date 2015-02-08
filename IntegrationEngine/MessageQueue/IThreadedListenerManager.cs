using Common.Logging;
using IntegrationEngine.Configuration;
using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.Jobs;
using IntegrationEngine.Core.Mail;
using IntegrationEngine.Core.Storage;
using Nest;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace IntegrationEngine.MessageQueue
{
    public interface IThreadedListenerManager : IDisposable
    {
        CancellationTokenSource CancellationTokenSource { get; set; }
        IMessageQueueListener MessageQueueListener { get; set; }
        ILog Log { get; set; }
        void StartListener();
    }
}
