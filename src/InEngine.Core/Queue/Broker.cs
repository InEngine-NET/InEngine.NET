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

        public ConnectionMultiplexer _redis;
        public ConnectionMultiplexer Redis
        {
            get
            {
                if (_redis == null)
                {
                    var redisConfig = ConfigurationOptions.Parse($"{RedisHost}:{RedisPort}");
                    redisConfig.Password = string.IsNullOrWhiteSpace(RedisPassword) ? null : RedisPassword;
                    _redis = ConnectionMultiplexer.Connect(redisConfig);
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
            Redis.GetDatabase(RedisDb).ListLeftPush(
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

            var stageMessageTask = Redis.GetDatabase(RedisDb).ListRightPopLeftPush(waitingQueueName, processingQueueName);
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
                Redis.GetDatabase(RedisDb).ListRemove(processingQueueName, serializedMessage, 1);
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
            return Redis.GetDatabase(RedisDb).ListLength(PrimaryWaitingQueueName);
        }

        public long GetPrimaryProcessingQueueLength()
        {
            return Redis.GetDatabase(RedisDb).ListLength(PrimaryProcessingQueueName);
        }

        public bool ClearPrimaryWaitingQueue()
        {
            return Redis.GetDatabase(RedisDb).KeyDelete(PrimaryWaitingQueueName);
        }

        public bool ClearPrimaryProcessingQueue()
        {
            return Redis.GetDatabase(RedisDb).KeyDelete(PrimaryProcessingQueueName);
        }
        #endregion

        #region Secondary Queue Management Methods
        public long GetSecondaryWaitingQueueLength()
        {
            return Redis.GetDatabase(RedisDb).ListLength(SecondaryWaitingQueueName);
        }

        public long GetSecondaryProcessingQueueLength()
        {
            return Redis.GetDatabase(RedisDb).ListLength(SecondaryProcessingQueueName);
        }

        public bool ClearSecondaryWaitingQueue()
        {
            return Redis.GetDatabase(RedisDb).KeyDelete(SecondaryWaitingQueueName);
        }

        public bool ClearSecondaryProcessingQueue()
        {
            return Redis.GetDatabase(RedisDb).KeyDelete(SecondaryProcessingQueueName);
        }
        #endregion
    }
}
