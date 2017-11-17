using System;
namespace InEngine.Core.Queue
{
    public class AbstractBrokerCommand : AbstractCommand, IBrokerConfiguration
    {
        public string QueueName { get; set; }
        public string RedisHost { get; set; }
        public int RedisDb { get; set; }
        public int RedisPort { get; set; }
        public string RedisPassword { get; set; }

        protected AbstractBrokerCommand()
        {
            QueueName = Config.QueueName;
            RedisHost = Config.RedisHost;
            RedisDb = Config.RedisDb;
            RedisPort = Config.RedisPort;
            RedisPassword = Config.RedisPassword;

            //QueueName = brokerConfiguration.QueueName;
            //RedisHost = brokerConfiguration.RedisHost;
            //RedisDb = brokerConfiguration.RedisDb;
            //RedisPort = brokerConfiguration.RedisPort;
            //RedisPassword = brokerConfiguration.RedisPassword;
        }
    }
}
