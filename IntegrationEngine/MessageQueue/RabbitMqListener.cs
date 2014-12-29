using System;
using System.Linq;
using System.Reflection;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using IntegrationEngine.Reports;
using System.Collections.Generic;

namespace IntegrationEngine.MessageQueue
{
    public class RabbitMqListener
    {
//        public IList<Assembly> AssembliesWithJobs { get; set; }
//
//        public RabbitMqListener()
//        {}
//
//        public RabbitMqListener(IList<Assembly> assembliesWithJobs) : this()
//        {
//            AssembliesWithJobs = assembliesWithJobs;
//        }

        public static void Listen(IList<Assembly> assembliesWithJobs)
        {
            var connectionFactory = new ConnectionFactory() { 
                HostName = "localhost",
                UserName = "guest",
                Password = "guest",
                VirtualHost = "/",
                Protocol = Protocols.DefaultProtocol,
                Port = AmqpTcpEndpoint.UseDefaultPort,
            };

            using (var connection = connectionFactory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    var consumer = new QueueingBasicConsumer(channel);
                    channel.BasicConsume("myqueue", true, consumer);
                    Console.WriteLine(" [*] Waiting for messages. To exit press CTRL+C");
                    while (true)
                    {
                        var eventArgs = (BasicDeliverEventArgs)consumer.Queue.Dequeue();
                        var body = eventArgs.Body;
                        var message = Encoding.UTF8.GetString(body);
                        Console.WriteLine(" [x] Received {0}", message);
                        var types = assembliesWithJobs
                            .SelectMany(x => x.GetTypes())
                            .Where(x => typeof(IIntegrationJob).IsAssignableFrom(x) && x.IsClass);
                        if (!types.Any())
                            continue;
                        var type = types.FirstOrDefault(t => t.FullName.Equals(message));
                        var reportJob = Activator.CreateInstance(type) as IIntegrationJob;
                        if (reportJob != null)
                            reportJob.Run();
                    }
                }
            }
        }
    }
}
