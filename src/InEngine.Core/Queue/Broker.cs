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
        public string PrimaryWaitingQueueName { get { return QueueBaseName + ":PrimaryWaiting"; } }
        public string PrimaryProcessingQueueName { get { return QueueBaseName + ":PrimaryProcessing"; } }
        public string SecondaryWaitingQueueName { get { return QueueBaseName + ":SecondaryWaiting"; } }
        public string SecondaryProcessingQueueName { get { return QueueBaseName + ":SecondaryProcessing"; } }

        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() => { 
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
                Redis.ListRemove(processingQueueName, serializedMessage, 1);
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
            return Redis.ListLength(PrimaryWaitingQueueName);
        }

        public long GetPrimaryProcessingQueueLength()
        {
            return Redis.ListLength(PrimaryProcessingQueueName);
        }

        public bool ClearPrimaryWaitingQueue()
        {
            return Redis.KeyDelete(PrimaryWaitingQueueName);
        }

        public bool ClearPrimaryProcessingQueue()
        {
            return Redis.KeyDelete(PrimaryProcessingQueueName);
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

        public bool ClearSecondaryWaitingQueue()
        {
            return Redis.KeyDelete(SecondaryWaitingQueueName);
        }

        public bool ClearSecondaryProcessingQueue()
        {
            return Redis.KeyDelete(SecondaryProcessingQueueName);
        }
        #endregion
    }
}
