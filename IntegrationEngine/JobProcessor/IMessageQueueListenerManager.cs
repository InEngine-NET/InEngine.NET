using Common.Logging;
using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.IntegrationJob;
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
using System.Threading.Tasks;

namespace IntegrationEngine.JobProcessor
{
    public interface IMessageQueueListenerManager : IDisposable
    {
        CancellationTokenSource CancellationTokenSource { get; set; }
        MessageQueueListenerFactory MessageQueueListenerFactory { get; set; }
        ILog Log { get; set; }
        Task StartListener();
    }
}
