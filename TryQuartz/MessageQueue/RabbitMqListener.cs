using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace TryQuartz.MessageQueue
{
    public class RabbitMqListener
    {
        public static void Listen()
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
                        var rscriptLauncher = ContainerSingleton.GetContainer().Resolve<RScriptLauncher>();
                        rscriptLauncher.Run();
                    }
                }
            }
        }
    }
}

