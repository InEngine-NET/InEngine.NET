using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using InEngine.Core.Exceptions;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace InEngine.Core.Queuing
{
    public class Broker
    {
        public string QueueBaseName { get; set; } = "InEngine:Queue";
        public string QueueName { get; internal set; } = "Primary";
        public string PendingQueueName { get { return QueueBaseName + $":{QueueName}:Pending"; } }
        public string InProgressQueueName { get { return QueueBaseName + $":{QueueName}:InProgress"; } }
        public string FailedQueueName { get { return QueueBaseName + $":{QueueName}:Failed"; } }
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
        public bool UseCompression { get; set; }
        public int RedisDb { get; set; }

        public Broker()
        {}

        public Broker(bool useSecondaryQueue) : this()
        {
            if (useSecondaryQueue)
                QueueName = "Secondary";
        }

        public static Broker Make(bool useSecondaryQueue = false)
        {
            var queueSettings = InEngineSettings.Make().Queue;
            return new Broker(useSecondaryQueue)
            {
                QueueBaseName = queueSettings.QueueName,
                UseCompression = queueSettings.UseCompression,
                RedisDb = queueSettings.RedisDb
            };
        }

        public void Publish(ICommand command)
        {
            var serializedCommand = JsonConvert.SerializeObject(command);
            Redis.ListLeftPush(
                PendingQueueName,
                new Message() {
                    IsCompressed = UseCompression,
                    CommandClassName = command.GetType().FullName,
                    CommandAssemblyName = command.GetType().Assembly.GetName().Name + ".dll",
                    SerializedCommand = UseCompression ? serializedCommand.Compress() : serializedCommand
            }.SerializeToJson()
            );
        }

        public bool Consume()
        {
            var stageMessageTask = Redis.ListRightPopLeftPush(PendingQueueName, InProgressQueueName);
            var serializedMessage = stageMessageTask.ToString();
            if (serializedMessage == null)
                return false;
            var message = serializedMessage.DeserializeFromJson<Message>();
            if (message == null)
                return false;
            var commandInstance = ExtractCommandInstanceFromMessage(message);

            try
            {
                commandInstance.Run();
            }
            catch (Exception exception)
            {
                Redis.ListRemove(InProgressQueueName, serializedMessage, 1);
                Redis.ListLeftPush(FailedQueueName, stageMessageTask);
                throw new CommandFailedException("Consumed command failed.", exception);
            }

            try
            {
                Redis.ListRemove(InProgressQueueName, serializedMessage, 1);
            }
            catch (Exception exception)
            {
                throw new CommandFailedException($"Failed to remove completed message from queue: {InProgressQueueName}", exception);
            }

            return true;
        }

        public static ICommand ExtractCommandInstanceFromMessage(Message message)
        {
            var commandType = Type.GetType($"{message.CommandClassName}, {message.CommandAssemblyName}");
            if (commandType == null)
                throw new CommandFailedException("Could not locate command type.");
            if (message.IsCompressed)
                return JsonConvert.DeserializeObject(message.SerializedCommand.Decompress(), commandType) as ICommand;
            return JsonConvert.DeserializeObject(message.SerializedCommand, commandType) as ICommand;
        }

        #region Queue Management Methods
        public long GetPendingQueueLength()
        {
            return Redis.ListLength(PendingQueueName);
        }

        public long GetInProgressQueueLength()
        {
            return Redis.ListLength(InProgressQueueName);
        }

        public long GetFailedQueueLength()
        {
            return Redis.ListLength(FailedQueueName);
        }

        public bool ClearPendingQueue()
        {
            return Redis.KeyDelete(PendingQueueName);
        }

        public bool ClearInProgressQueue()
        {
            return Redis.KeyDelete(InProgressQueueName);
        }

        public bool ClearFailedQueue()
        {
            return Redis.KeyDelete(FailedQueueName);
        }

        public void RepublishFailedMessages()
        {
            Redis.ListRightPopLeftPush(FailedQueueName, PendingQueueName);
        }

        public List<Message> PeekPendingMessages(long from, long to)
        {
            return GetMessages(PendingQueueName, from, to);
        }

        public List<Message> PeekInProgressMessages(long from, long to)
        {
            return GetMessages(InProgressQueueName, from, to);
        }

        public List<Message> PeekFailedMessages(long from, long to)
        {
            return GetMessages(FailedQueueName, from, to);
        }

        public List<Message> GetMessages(string queueName, long from, long to)
        {
            return Redis.ListRange(queueName, from, to)
                        .ToStringArray()
                        .Select(x => x.DeserializeFromJson<Message>()).ToList();
        }
        #endregion
    }
}
