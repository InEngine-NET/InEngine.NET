using BeekmanLabs.UnitTesting;
using Common.Logging;
using IntegrationEngine.Configuration;
using IntegrationEngine.MessageQueue;
using Moq;
using NUnit.Framework;
using System;
using RabbitMQ.Client;
using System.Text;

namespace IntegrationEngine.Tests.MessageQueue
{
    public class RabbitMQClientTest : TestBase<RabbitMQClient>
    {
        [Test]
        public void ShouldPublishDispatchMessage()
        {
            var config = new RabbitMQConfiguration() {
                QueueName = "MyQueue",
                ExchangeName = "MyExchange",
            };
            Subject.MessageQueueConfiguration = config;
            var expected = new IntegrationJobStub();
            var mockLog = new Mock<ILog>();
            Subject.Log = mockLog.Object;
            var mockMessageQueueConnection = new Mock<IMessageQueueConnection>();
            Subject.MessageQueueConnection = mockMessageQueueConnection.Object;
            var mockConnection = new Mock<IConnection>();
            mockMessageQueueConnection.Setup(x => x.GetConnection()).Returns(mockConnection.Object);
            var mockModel = new Mock<IModel>();
            mockConnection.Setup(x => x.CreateModel()).Returns(mockModel.Object);
            mockModel.Setup(x => x.QueueBind(config.QueueName, config.ExchangeName, ""));
            mockModel.Setup(x => x.BasicPublish(config.ExchangeName, "", null, It.IsAny<byte[]>()));

            Subject.Publish(expected, null);

            mockMessageQueueConnection.Verify(x => x.GetConnection(), Times.Once);
            mockConnection.Verify(x => x.CreateModel(), Times.Once);
            mockModel.Verify(x => x.QueueBind(config.QueueName, config.ExchangeName, ""), Times.Once);
            mockModel.Verify(x => x.BasicPublish(config.ExchangeName, "", null, It.IsAny<byte[]>()), Times.Once);
        }
    }
}
