using CommandLine;

namespace InEngine.Core.Queue
{
    public interface IBrokerConfiguration
    {
        [Option('q', "queue-name", DefaultValue = "InEngine:Queue")]
        string QueueName { get; set; }

        [Option('r', "redis-host", DefaultValue = "localhost")]
        string RedisHost { get; set; }

        [Option('d', "redis-db", DefaultValue = 0)]
        int RedisDb { get; set; }

        [Option('p', "redis-port", DefaultValue = 6379)]
        int RedisPort { get; set; }

        [Option('m', "redis-password")]
        string RedisPassword { get; set; }
    }
}
