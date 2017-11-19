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
        public string PrimaryWaitingQueueName { get { return QueueBaseName + ":PrimaryWaiting"; } }
        public string PrimaryProcessingQueueName { get { return QueueBaseName + ":PrimaryProcessing"; } }
        public string SecondaryWaitingQueueName { get { return QueueBaseName + ":SecondaryWaiting"; } }
        public string SecondaryProcessingQueueName { get { return QueueBaseName + ":SecondaryProcessing"; } }

        public IRedisClient _redis;
        public IRedisClient Redis
        {
            get
            {
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
            return new Broker()
            {
                QueueBaseName = queueSettings.QueueName,
                RedisHost = queueSettings.RedisHost,
                RedisPort = queueSettings.RedisPort,
                RedisDb = queueSettings.RedisDb,
                RedisPassword = queueSettings.RedisPassword,
            };
        }

        public void Publish(ICommand command, bool useSecondaryQueue = false)
        {
            Redis.LPushAsync(useSecondaryQueue ? SecondaryWaitingQueueName : PrimaryWaitingQueueName,
                new Message()
                {
                    CommandClassName = command.GetType().FullName,
                    CommandAssemblyName = command.GetType().Assembly.GetName().Name + ".dll",
                    SerializedCommand = JsonConvert.SerializeObject(command)
                });
        }

        public bool Consume(bool useSecondaryQueue = false)
        {
            var waitingQueueName = useSecondaryQueue ? SecondaryWaitingQueueName : PrimaryWaitingQueueName;
            var processingQueueName = useSecondaryQueue ? SecondaryProcessingQueueName : PrimaryProcessingQueueName;
            var stageMessageTask = Redis.RPopLPushAsync(waitingQueueName, processingQueueName);
            stageMessageTask.Wait();
            var message = stageMessageTask.Result.As<Message>();
            if (message == null)
                return false;

            var commandType = Type.GetType($"{message.CommandClassName}, {message.CommandAssemblyName}");
            if (commandType == null)
                throw new CommandFailedException("Consumed command failed: could not locate command type.");
            var commandInstance = JsonConvert.DeserializeObject(message.SerializedCommand, commandType) as ICommand;

            try
            {
                commandInstance.Run();
                Redis.LRemAsync(processingQueueName, 1, message).Wait();
            }
            catch (Exception exception)
            {
                throw new CommandFailedException("Consumed command failed.", exception);
            }
            return true;
        }

        #region Primary Queue Management Methods
        public long GetPrimaryWaitingQueueLength()
        {
            return WaitAndReturnResult(Redis.LLenAsync(PrimaryWaitingQueueName));
        }

        public long GetPrimaryProcessingQueueLength()
        {
            return WaitAndReturnResult(Redis.LLenAsync(PrimaryProcessingQueueName));
        }

        public long ClearPrimaryWaitingQueue()
        {
            return WaitAndReturnResult(Redis.DelAsync(SecondaryWaitingQueueName));
        }

        public long ClearPrimaryProcessingQueue()
        {
            return WaitAndReturnResult(Redis.DelAsync(SecondaryProcessingQueueName));
        }
        #endregion

        #region Secondary Queue Management Methods
        public long GetSecondaryWaitingQueueLength()
        {
            return WaitAndReturnResult(Redis.LLenAsync(PrimaryWaitingQueueName));
        }

        public long GetSecondaryProcessingQueueLength()
        {
            return WaitAndReturnResult(Redis.LLenAsync(PrimaryProcessingQueueName));
        }

        public long ClearSecondaryWaitingQueue()
        {
            return WaitAndReturnResult(Redis.DelAsync(SecondaryWaitingQueueName));
        }

        public long ClearSecondaryProcessingQueue()
        {
            return WaitAndReturnResult(Redis.DelAsync(SecondaryProcessingQueueName));
        }
        #endregion


        public long WaitAndReturnResult(Task<long> task)
        {
            task.Wait();
            return task.Result;
        }
    }
}
