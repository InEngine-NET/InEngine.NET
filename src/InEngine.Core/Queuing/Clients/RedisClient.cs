using System;
using System.Collections.Generic;
using System.Linq;
using InEngine.Core.Commands;
using InEngine.Core.Exceptions;
using StackExchange.Redis;

namespace InEngine.Core.Queuing.Clients
{
    public class RedisClient : IQueueClient
    {
        public string QueueBaseName { get; set; } = "InEngineQueue";
        public string QueueName { get; set; } = "Primary";
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
        public IDatabase Redis { get { return Connection.GetDatabase(RedisDb); } }
        public bool UseCompression { get; set; }
        public int RedisDb { get; set; }

        public void Publish(ICommand command)
        {
            Redis.ListLeftPush(
                PendingQueueName,
                new Message() {
                    IsCompressed = UseCompression,
                    CommandClassName = command.GetType().FullName,
                    CommandAssemblyName = command.GetType().Assembly.GetName().Name + ".dll",
                    SerializedCommand = command.SerializeToJson(UseCompression)
                }.SerializeToJson()
            );
        }

        public bool Consume()
        {
            var rawRedisMessageValue = Redis.ListRightPopLeftPush(PendingQueueName, InProgressQueueName);
            var serializedMessage = rawRedisMessageValue.ToString();
            if (serializedMessage == null)
                return false;
            var message = serializedMessage.DeserializeFromJson<Message>();
            if (message == null)
                return false;
            var commandInstance = Queue.ExtractCommandInstanceFromMessage(message);

            try
            {
                commandInstance.Run();
            }
            catch (Exception exception)
            {
                Redis.ListRemove(InProgressQueueName, serializedMessage, 1);
                Redis.ListLeftPush(FailedQueueName, rawRedisMessageValue);
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

        public List<IMessage> PeekPendingMessages(long from, long to)
        {
            return GetMessages(PendingQueueName, from, to);
        }

        public List<IMessage> PeekInProgressMessages(long from, long to)
        {
            return GetMessages(InProgressQueueName, from, to);
        }

        public List<IMessage> PeekFailedMessages(long from, long to)
        {
            return GetMessages(FailedQueueName, from, to);
        }

        public List<IMessage> GetMessages(string queueName, long from, long to)
        {
            return Redis.ListRange(queueName, from, to)
                        .ToStringArray()
                        .Select(x => x.DeserializeFromJson<IMessage>()).ToList();
        }
    }
}
