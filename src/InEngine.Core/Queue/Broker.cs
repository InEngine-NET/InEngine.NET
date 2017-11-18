using System;
using System.Reflection;
using System.Threading.Tasks;
using InEngine.Core.Exceptions;
using Newtonsoft.Json;
using RedisBoost;

namespace InEngine.Core.Queue
{
    public class Broker
    {
        public string QueueBaseName { get; set; } = "InEngine:Queue";
        public string WaitingQueueName { get { return QueueBaseName + ":Waiting"; } }
        public string ProcessingQueueName { get { return QueueBaseName + ":Processing"; } }
        public IRedisClient _redis;
        public IRedisClient Redis { 
            get {
                if (_redis == null)
                {
                    var connectionTask = RedisClient.ConnectAsync(RedisHost, RedisPort, RedisDb);
                    connectionTask.Wait();
                    _redis = connectionTask.Result;   
                }
                return _redis;
            } 
        }
        public string RedisHost { get; set; }
        public int RedisDb { get; set; }
        public int RedisPort { get; set; }
        public string RedisPassword { get; set; }

        public static Broker Make()
        {
            var queueSettings = InEngineSettings.Make().Queue;
            return new Broker() {
                QueueBaseName = queueSettings.Name,
                RedisHost = queueSettings.RedisHost,
                RedisPort = queueSettings.RedisPort,
                RedisDb = queueSettings.RedisDb,
                RedisPassword = queueSettings.RedisPassword,
            };
        }

        public void Publish(ICommand command)
        {
            Redis.LPushAsync(WaitingQueueName, new Message() {
                CommandClassName = command.GetType().FullName,
                CommandAssemblyName = command.GetType().Assembly.GetName().Name + ".dll",
                SerializedCommand = JsonConvert.SerializeObject(command)
            });
        }

        public bool Consume()
        {
            var stageMessageTask = Redis.RPopLPushAsync(WaitingQueueName, ProcessingQueueName);
            stageMessageTask.Wait();
            var message = stageMessageTask.Result.As<Message>();
            if (message == null)
                return false;

            //message.CommandClassName
            var command = Assembly.LoadFrom(message.CommandAssemblyName)
                                  .CreateInstance(message.CommandClassName) as ICommand;

            var commandType = Type.GetType(message.CommandClassName);
            var commandInstance = JsonConvert.DeserializeObject(message.SerializedCommand, commandType) as ICommand;

            try
            {
                command.Run();
                Redis.LRemAsync(ProcessingQueueName, 1, message).Wait();
            }
            catch(Exception exception)
            {
                throw new CommandFailedException("Consumed command failed.", exception);
            }
            return true;
        }

        public long GetWaitingQueueLength()
        {
            return WaitAndReturnResult(Redis.LLenAsync(WaitingQueueName));
        }

        public long GetProcessingQueueLength()
        {
            return WaitAndReturnResult(Redis.LLenAsync(ProcessingQueueName));
        }

        public long ClearWaitingQueue()
        {
            return WaitAndReturnResult(Redis.DelAsync(WaitingQueueName));
        }

        public long ClearProcessingQueue()
        {
            return WaitAndReturnResult(Redis.DelAsync(ProcessingQueueName));
        }

        public long WaitAndReturnResult(Task<long> task)
        {
            task.Wait();
            return task.Result;
        }
    }
}
