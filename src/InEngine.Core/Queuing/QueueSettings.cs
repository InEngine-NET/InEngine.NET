namespace InEngine.Core.Queuing
{
    public class QueueSettings
    {
        public bool UseCompression { get; set; }
        public int PrimaryQueueConsumers { get; set; } = 8;
        public int SecondaryQueueConsumers { get; set; } = 8;
        public string QueueDriver { get; set; }
        public string QueueName { get; set; }
        public string RedisHost { get; set; }
        public int RedisPort { get; set; }
        public int RedisDb { get; set; }
        public string RedisPassword { get; set; }
    }
}
