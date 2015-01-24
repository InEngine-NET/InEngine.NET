﻿using Common.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using MSMessageQueue = System.Messaging.MessageQueue;

namespace IntegrationEngine.MessageQueue
{
    public class MsmqClient : IMessageQueueClient
    {
        public MSMessageQueue MSMessageQueue { get; set; }
        string _queueName { get; set; }
        public string QueueName { 
            get { return _queueName; }
            set {
                if (!MSMessageQueue.Exists(QueueName))
                    MSMessageQueue.Create(QueueName);
                _queueName = value;
            }
        }
        public ILog Log { get; set; }

        public void Publish<T>(T value, IDictionary<string, string> parameters)
        {
            try
            {
                var message = value.GetType().FullName;
                var body = Encoding.UTF8.GetBytes(message);
                MSMessageQueue.Send(body);
                Log.Debug(x => x("Sent message: {0}", message));
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        public bool IsServerAvailable()
        {
            try
            {
                return MSMessageQueue.Exists(QueueName);
            }
            catch (Exception exception)
            {
                Log.Error(exception);
                return false;
            }
        }
    }
}
