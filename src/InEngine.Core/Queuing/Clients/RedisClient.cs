using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InEngine.Core.Exceptions;
using InEngine.Core.IO;
using InEngine.Core.Queuing.Message;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;

namespace InEngine.Core.Queuing.Clients;

public class RedisClient : IQueueClient
{
    public static RedisClientSettings ClientSettings { get; set; }
    public MailSettings MailSettings { get; set; }

    public ILogger Log { get; set; } = LogManager.GetLogger<RedisClient>();
    public int Id { get; set; } = 0;
    public string QueueBaseName { get; set; } = "InEngineQueue";
    public string QueueName { get; set; } = "Primary";

    public string RecoveryQueueName => QueueBaseName + $":{QueueName}:Recovery";

    public string PendingQueueName => QueueBaseName + $":{QueueName}:Pending";

    public string InProgressQueueName => QueueBaseName + $":{QueueName}:InProgress";

    public string FailedQueueName => QueueBaseName + $":{QueueName}:Failed";

    public static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
    {
        var redisConfig = ConfigurationOptions.Parse($"{ClientSettings.Host}:{ClientSettings.Port}");
        redisConfig.Password = string.IsNullOrWhiteSpace(ClientSettings.Password) ? null : ClientSettings.Password;
        redisConfig.AbortOnConnectFail = false;
        return ConnectionMultiplexer.Connect(redisConfig);
    });

    public static ConnectionMultiplexer Connection => lazyConnection.Value;

    public ConnectionMultiplexer _connectionMultiplexer;
    private bool isDisposed;

    public IDatabase Redis => Connection.GetDatabase(ClientSettings.Database);

    public bool UseCompression { get; set; }
    public RedisChannel RedisChannel { get; set; }

    public void InitChannel()
    {
        if (RedisChannel.IsNullOrEmpty)
            RedisChannel = new RedisChannel(QueueBaseName, RedisChannel.PatternMode.Auto);
    }

    public void Publish(AbstractCommand command)
    {
        InitChannel();
        Redis.ListLeftPush(
            PendingQueueName,
            new CommandEnvelope()
            {
                IsCompressed = UseCompression,
                CommandClassName = command.GetType().FullName,
                PluginName = command.GetType().Assembly.GetName().Name,
                SerializedCommand = command.SerializeToJson(UseCompression)
            }.SerializeToJson()
        );

        PublishToChannel($"published command: {command.Name}");
    }

    public long PublishToChannel(string message = "published command.")
    {
        InitChannel();
        return Connection.GetSubscriber().Publish(RedisChannel, message);
    }

    public void Recover()
    {
        for (var i = 0; i < Redis.ListLength(PendingQueueName); i++)
            PublishToChannel();
    }

    public void Consume(CancellationToken cancellationToken)
    {
        try
        {
            InitChannel();
            Connection.GetSubscriber().Subscribe(RedisChannel,
                delegate
                {
                    Task.Factory.StartNew(Consume, cancellationToken, TaskCreationOptions.LongRunning,
                        TaskScheduler.Default);
                });
        }
        catch (OperationCanceledException exception)
        {
            Log.LogError(exception, "OperationCanceledException while consuming message");
        }
        catch (Exception exception)
        {
            Log.LogError(exception, "Generic error while consuming message");
        }
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

        var command = commandEnvelope.GetCommandInstanceAndIncrementRetry(() =>
        {
            Redis.ListLeftPush(FailedQueueName, commandEnvelope.SerializeToJson());
        });

        try
        {
            command.WriteSummaryToConsole();
            command.RunWithLifeCycle();
        }
        catch (Exception exception)
        {
            Log.LogError(exception, "Error running task");
            Redis.ListRemove(InProgressQueueName, serializedMessage, 1);
            if (command.CommandLifeCycle.ShouldRetry())
                Redis.ListLeftPush(PendingQueueName, commandEnvelope.SerializeToJson());
            else
            {
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
            const string message = "Failed to remove completed commandEnvelope from queue";
            Log.LogError(exception, message);
            throw new CommandFailedException(message, exception);
        }

        return commandEnvelope;
    }

    public bool ClearPendingQueue() => Redis.KeyDelete(PendingQueueName);

    public bool ClearInProgressQueue() => Redis.KeyDelete(InProgressQueueName);

    public bool ClearFailedQueue() => Redis.KeyDelete(FailedQueueName);

    public void RepublishFailedMessages() => Redis.ListRightPopLeftPush(FailedQueueName, PendingQueueName);

    public List<ICommandEnvelope> PeekPendingMessages(long from, long to) => GetMessages(PendingQueueName, from, to);

    public List<ICommandEnvelope> PeekInProgressMessages(long from, long to) =>
        GetMessages(InProgressQueueName, from, to);

    public List<ICommandEnvelope> PeekFailedMessages(long from, long to) => GetMessages(FailedQueueName, from, to);

    public List<ICommandEnvelope> GetMessages(string queueName, long from, long to) =>
        Redis.ListRange(queueName, from, to)
            .ToStringArray()
            .Select(x => x.DeserializeFromJson<CommandEnvelope>() as ICommandEnvelope).ToList();

    public Dictionary<string, long> GetQueueLengths()
    {
        return new Dictionary<string, long>
        {
            { "Pending", Redis.ListLength(PendingQueueName) },
            { "In-progress", Redis.ListLength(InProgressQueueName) },
            { "Failed", Redis.ListLength(FailedQueueName) }
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (isDisposed) 
            return;
        if (!disposing) 
            return;
        _connectionMultiplexer?.Dispose();
        isDisposed = true;
    }
}