using System;
namespace InEngine.Core.Queue
{
    public class QueueSettings
    {
        public string QueueName { get; set; }
        public string RedisHost { get; set; }
        public int RedisPort { get; set; }
        public int RedisDb { get; set; }
        public string RedisPassword { get; set; }
    }
}
