﻿using Common.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using MSMessageQueue = System.Messaging.MessageQueue;

namespace IntegrationEngine.Core.MessageQueue
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

        public void Publish(byte[] message)
        {
            MSMessageQueue.Send(message);
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
