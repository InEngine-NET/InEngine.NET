using Common.Logging;
using IntegrationEngine.Core.Jobs;
using IntegrationEngine.Core.MessageQueue;
using IntegrationEngine.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace IntegrationEngine.Scheduler
{
    public class Dispatcher : IDispatcher
    {
        public IMessageQueueClient MessageQueueClient { get; set; }
        public ILog Log { get; set; }

        public Dispatcher() 
        {
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public void Dispatch<T>(T value, IDictionary<string, string> parameters)
        {
            try
            {
                var type = value.GetType();
                if (type is IParameterizedJob)
                    (value as IParameterizedJob).Parameters = parameters;
                var message = JsonConvert.SerializeObject(new DispatchTrigger {
                    JobType = type.FullName,
                    Parameters = parameters,
                });
                MessageQueueClient.Publish(Encoding.UTF8.GetBytes(message));
                Log.Debug(x => x("Sent message: {0}", message));
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }
    }
}
