using Common.Logging;
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

namespace IntegrationEngine.JobProcessor
{
    public class ThreadedListenerManager : IThreadedListenerManager
    {
        Thread listenerThread;
        public IMessageQueueListener MessageQueueListener { get; set; }
        public ILog Log { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }

        public ThreadedListenerManager()
        {
            CancellationTokenSource = new CancellationTokenSource();
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public void Dispose()
        {
            CancellationTokenSource.Cancel();
            MessageQueueListener.Dispose();
            listenerThread.Join();
        }

        void Listen()
        {
            MessageQueueListener.Listen(CancellationTokenSource.Token);
        }

        public void StartListener() 
        {
            if (listenerThread == null)
                listenerThread = new Thread(Listen);
            if (listenerThread.ThreadState == ThreadState.Running)
            {
                Log.Warn("Message queue listener already running.");
                return;
            }

            listenerThread.Start();
            Log.Info("Message queue listener started.");
        }
    }
}
