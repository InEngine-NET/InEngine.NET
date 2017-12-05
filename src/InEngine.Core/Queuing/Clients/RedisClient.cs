using System;
using System.Collections.Generic;
using System.Linq;
using InEngine.Core.Exceptions;
using InEngine.Core.IO;
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

        public void Publish(AbstractCommand command)
        {
            Redis.ListLeftPush(
                PendingQueueName,
                new CommandEnvelope() {
                    IsCompressed = UseCompression,
                    CommandClassName = command.GetType().FullName,
                    CommandAssemblyName = command.GetType().Assembly.GetName().Name + ".dll",
                    SerializedCommand = command.SerializeToJson(UseCompression)
                }.SerializeToJson()
            );
        }

        public ICommandEnvelope Consume()
        {
            var rawRedisMessageValue = Redis.ListRightPopLeftPush(PendingQueueName, InProgressQueueName);
            var serializedMessage = rawRedisMessageValue.ToString();
            if (serializedMessage == null)
                return null;
            var commandEnvelope = serializedMessage.DeserializeFromJson<CommandEnvelope>();
            if (commandEnvelope == null)
                throw new CommandFailedException("Could not deserialize the command.");
            
            var command = QueueAdapter.ExtractCommandInstanceFromMessage(commandEnvelope);
            command.CommandLifeCycle.IncrementRetry();
            commandEnvelope.SerializedCommand = command.SerializeToJson(UseCompression);
            try
            {
                command.WriteSummaryToConsole();
                command.RunWithLifeCycle();
            }
            catch (Exception exception)
            {
                Redis.ListRemove(InProgressQueueName, serializedMessage, 1);
                if (command.CommandLifeCycle.ShouldRetry())
                    Redis.ListLeftPush(PendingQueueName, commandEnvelope.SerializeToJson());
                else {
                    Redis.ListLeftPush(FailedQueueName, commandEnvelope.SerializeToJson());
                    throw new CommandFailedException("Failed to run consumed command.", exception);   
                }
            }

            try
            {
                Redis.ListRemove(InProgressQueueName, serializedMessage, 1);
            }
            catch (Exception exception)
            {
                throw new CommandFailedException($"Failed to remove completed commandEnvelope from queue: {InProgressQueueName}", exception);
            }

            return commandEnvelope;
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

        public List<ICommandEnvelope> PeekPendingMessages(long from, long to)
        {
            return GetMessages(PendingQueueName, from, to);
        }

        public List<ICommandEnvelope> PeekInProgressMessages(long from, long to)
        {
            return GetMessages(InProgressQueueName, from, to);
        }

        public List<ICommandEnvelope> PeekFailedMessages(long from, long to)
        {
            return GetMessages(FailedQueueName, from, to);
        }

        public List<ICommandEnvelope> GetMessages(string queueName, long from, long to)
        {
            return Redis.ListRange(queueName, from, to)
                        .ToStringArray()
                        .Select(x => x.DeserializeFromJson<CommandEnvelope>() as ICommandEnvelope).ToList();
        }
    }
}
