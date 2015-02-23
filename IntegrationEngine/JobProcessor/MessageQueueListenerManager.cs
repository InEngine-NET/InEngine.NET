using Common.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationEngine.JobProcessor
{
    public class MessageQueueListenerManager : IMessageQueueListenerManager
    {
        public MessageQueueListenerFactory MessageQueueListenerFactory { get; set; }
        public ILog Log { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public int ListenerTaskCount { get; set; }
        IList<IMessageQueueListener> _listeners;

        public MessageQueueListenerManager()
        {
            _listeners = new List<IMessageQueueListener>();
            ListenerTaskCount = 2;
            CancellationTokenSource = new CancellationTokenSource();
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public async Task StartListener()
        {
            var tasks = Enumerable.Range(0, ListenerTaskCount).Select((i) => {
                return Task.Factory.StartNew(() => {
                    var listener = MessageQueueListenerFactory.CreateRabbitMQListener();
                    _listeners.Add(listener);
                    listener.Listen(CancellationTokenSource.Token, i);
                }, 
                    TaskCreationOptions.LongRunning);
            }).ToList();
            await Task.WhenAll(tasks);
        }

        public void Dispose()
        {
            CancellationTokenSource.Cancel();
            foreach (var listener in _listeners)
                if (listener != null)
                    listener.Dispose();
        }
    }
}

