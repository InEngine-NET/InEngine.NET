using InEngine.Core.Queuing.Clients;

namespace InEngine.Core.Queuing;

public class QueueSettings
{
    public bool UseCompression { get; set; }
    public int PrimaryQueueConsumers { get; set; } = 8;
    public int SecondaryQueueConsumers { get; set; } = 8;
    public string QueueDriver { get; set; }
    public string QueueName { get; set; }
    public RedisClientSettings Redis { get; set; }
    public RabbitMQClientSettings RabbitMQ { get; set; }
    public FileClientSettings File { get; set; }
}