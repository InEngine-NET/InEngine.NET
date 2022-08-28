using System;
using System.Collections.Generic;
using System.Threading;
using InEngine.Core.IO;
using InEngine.Core.Queuing.Clients;
using InEngine.Core.Queuing.Message;
using Microsoft.Extensions.Logging;

namespace InEngine.Core.Queuing;

public class QueueAdapter : IQueueClient
{
    public ILogger Log { get; set; } = LogManager.GetLogger<QueueAdapter>();

    public int Id
    {
        get => QueueClient.Id;
        set => QueueClient.Id = value;
    }

    public IQueueClient QueueClient { get; set; }

    public string QueueBaseName
    {
        get => QueueClient.QueueBaseName;
        set => QueueClient.QueueBaseName = value;
    }

    public string QueueName
    {
        get => QueueClient.QueueName;
        set => QueueClient.QueueName = value;
    }

    public bool UseCompression
    {
        get => QueueClient.UseCompression;
        set => QueueClient.UseCompression = value;
    }

    public MailSettings MailSettings
    {
        get => QueueClient.MailSettings;
        set => QueueClient.MailSettings = value;
    }

    public static QueueAdapter Make(bool useSecondaryQueue, QueueSettings queueSettings, MailSettings mailSettings)
    {
        var queueDriverName = queueSettings.QueueDriver.ToLower();
        var queue = new QueueAdapter();

        switch (queueDriverName)
        {
            case "redis":
                RedisClient.ClientSettings = queueSettings.Redis;
                queue.QueueClient = new RedisClient()
                {
                    QueueBaseName = queueSettings.QueueName,
                    UseCompression = queueSettings.UseCompression,
                };
                break;
            case "rabbitmq":
                RabbitMqClient.ClientSettings = queueSettings.RabbitMQ;
                queue.QueueClient = new RabbitMqClient()
                {
                    QueueBaseName = queueSettings.QueueName,
                    UseCompression = queueSettings.UseCompression
                };
                break;
            case "file":
                FileClient.ClientSettings = queueSettings.File;
                queue.QueueClient = new FileClient()
                {
                    QueueBaseName = queueSettings.QueueName,
                    UseCompression = queueSettings.UseCompression
                };
                break;
            case "sync":
                queue.QueueClient = new SyncClient();
                break;
            default:
                throw new Exception("Unspecified or unknown queue driver.");
        }

        queue.QueueName = useSecondaryQueue ? "Secondary" : "Primary";
        queue.MailSettings = mailSettings;
        return queue;
    }

    public void Publish(AbstractCommand command) => QueueClient.Publish(command);
    public void Consume(CancellationToken cancellationToken) => QueueClient.Consume(cancellationToken);
    public ICommandEnvelope Consume() => QueueClient.Consume();
    public void Recover() => QueueClient.Recover();
    public bool ClearPendingQueue() => QueueClient.ClearPendingQueue();
    public bool ClearInProgressQueue() => QueueClient.ClearInProgressQueue();
    public bool ClearFailedQueue() => QueueClient.ClearFailedQueue();
    public void RepublishFailedMessages() => QueueClient.RepublishFailedMessages();
    public List<ICommandEnvelope> PeekPendingMessages(long from, long to) => QueueClient.PeekPendingMessages(from, to);
    public List<ICommandEnvelope> PeekInProgressMessages(long from, long to) =>
        QueueClient.PeekInProgressMessages(from, to);
    public List<ICommandEnvelope> PeekFailedMessages(long from, long to) => QueueClient.PeekFailedMessages(from, to);
    public Dictionary<string, long> GetQueueLengths() => QueueClient.GetQueueLengths();
}