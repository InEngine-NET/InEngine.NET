using System;
using System.Reflection;
using System.Threading.Tasks;
using InEngine.Core.Exceptions;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace InEngine.Core.Queue
{
    public class Broker
    {
        public string QueueBaseName { get; set; } = "InEngine:Queue";
        public string PrimaryWaitingQueueName { get { return QueueBaseName + ":Primary:Waiting"; } }
        public string PrimaryProcessingQueueName { get { return QueueBaseName + ":Primary:Processing"; } }
        public string PrimaryFailedQueueName { get { return QueueBaseName + ":Primary:Failed"; } }
        public string SecondaryWaitingQueueName { get { return QueueBaseName + ":Secondary:Waiting"; } }
        public string SecondaryProcessingQueueName { get { return QueueBaseName + ":Secondary:Processing"; } }
        public string SecondaryFailedQueueName { get { return QueueBaseName + ":Secondary:Failed"; } }
        public static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() => { 
            var queueSettings = InEngineSettings.Make().Queue;
            var redisConfig = ConfigurationOptions.Parse($"{queueSettings.RedisHost}:{queueSettings.RedisPort}");
            redisConfig.Password = string.IsNullOrWhiteSpace(queueSettings.RedisPassword) ? 
                null : 
                queueSettings.RedisPassword;
            redisConfig.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(redisConfig); 
        });
        public static ConnectionMultiplexer Connection { get { return lazyConnection.Value; } } 
        public ConnectionMultiplexer _connectionMultiplexer;
        public IDatabase Redis
        {
            get
            {
                return Connection.GetDatabase(RedisDb);
            }
        }
        public static string RedisHost { get; set; }
        public static int RedisDb { get; set; }
        public static int RedisPort { get; set; }
        public static string RedisPassword { get; set; }

        public static Broker Make()
        {
            return new Broker() {
                QueueBaseName = InEngineSettings.Make().Queue.QueueName
            };
        }

        public void Publish(ICommand command, bool useSecondaryQueue = false)
        {
            Redis.ListLeftPush(
                useSecondaryQueue ? SecondaryWaitingQueueName : PrimaryWaitingQueueName,
                new Message() {
                    CommandClassName = command.GetType().FullName,
                    CommandAssemblyName = command.GetType().Assembly.GetName().Name + ".dll",
                    SerializedCommand = JsonConvert.SerializeObject(command)
                }.SerializeToJson()
            );
        }

        public bool Consume(bool useSecondaryQueue = false)
        {
            var waitingQueueName = useSecondaryQueue ? SecondaryWaitingQueueName : PrimaryWaitingQueueName;
            var processingQueueName = useSecondaryQueue ? SecondaryProcessingQueueName : PrimaryProcessingQueueName;
            var failedQueueName = useSecondaryQueue ? SecondaryFailedQueueName : PrimaryFailedQueueName;

            var stageMessageTask = Redis.ListRightPopLeftPush(waitingQueueName, processingQueueName);
            var serializedMessage = stageMessageTask.ToString();
            if (serializedMessage == null)
                return false;
            var message = serializedMessage.DeserializeFromJson<Message>();
            if (message == null)
                return false;

            var commandType = Type.GetType($"{message.CommandClassName}, {message.CommandAssemblyName}");
            if (commandType == null)
                throw new CommandFailedException("Consumed command failed: could not locate command type.");
            var commandInstance = JsonConvert.DeserializeObject(message.SerializedCommand, commandType) as ICommand;

            try
            {
                commandInstance.Run();
            }
            catch (Exception exception)
            {
                Redis.ListRemove(processingQueueName, serializedMessage, 1);
                Redis.ListLeftPush(failedQueueName, stageMessageTask);
                throw new CommandFailedException("Consumed command failed.", exception);
            }

            try
            {
                Redis.ListRemove(processingQueueName, serializedMessage, 1);
            }
            catch (Exception exception)
            {
                throw new CommandFailedException($"Failed to remove completed message from queue: {processingQueueName}", exception);
            }

            return true;
        }

        #region Primary Queue Management Methods
        public long GetPrimaryWaitingQueueLength()
        {
            return Redis.ListLength(PrimaryWaitingQueueName);
        }

        public long GetPrimaryProcessingQueueLength()
        {
            return Redis.ListLength(PrimaryProcessingQueueName);
        }

        public long GetPrimaryFailedQueueLength()
        {
            return Redis.ListLength(PrimaryFailedQueueName);
        }

        public bool ClearPrimaryWaitingQueue()
        {
            return Redis.KeyDelete(PrimaryWaitingQueueName);
        }

        public bool ClearPrimaryProcessingQueue()
        {
            return Redis.KeyDelete(PrimaryProcessingQueueName);
        }

        public bool ClearPrimaryFailedQueue()
        {
            return Redis.KeyDelete(PrimaryFailedQueueName);
        }
        #endregion

        #region Secondary Queue Management Methods
        public long GetSecondaryWaitingQueueLength()
        {
            return Redis.ListLength(SecondaryWaitingQueueName);
        }

        public long GetSecondaryProcessingQueueLength()
        {
            return Redis.ListLength(SecondaryProcessingQueueName);
        }

        public long GetSecondaryFailedQueueLength()
        {
            return Redis.ListLength(SecondaryFailedQueueName);
        }

        public bool ClearSecondaryWaitingQueue()
        {
            return Redis.KeyDelete(SecondaryWaitingQueueName);
        }

        public bool ClearSecondaryProcessingQueue()
        {
            return Redis.KeyDelete(SecondaryProcessingQueueName);
        }


        public bool ClearSecondaryFailedQueue()
        {
            return Redis.KeyDelete(SecondaryFailedQueueName);
        }
        #endregion

        public void RepublishFailedMessages(bool useSecondaryQueue)
        {
            Redis.ListRightPopLeftPush(
                useSecondaryQueue ? SecondaryFailedQueueName : PrimaryFailedQueueName,
                useSecondaryQueue ? SecondaryWaitingQueueName : PrimaryWaitingQueueName
            );
        }
    }
}
