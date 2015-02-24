using BeekmanLabs.UnitTesting;
using IntegrationEngine.JobProcessor;
using NUnit.Framework;
using Moq;
using System;
using System.Threading;

namespace IntegrationEngine.Tests.JobProcessor
{
    public class MessageQueueListenerManagerTest : TestBase<MessageQueueListenerManager>
    {
        public Mock<MessageQueueListenerFactory> MockMessageQueueListenerFactory { get; set; }

        [SetUp]
        public void Setup()
        {
            MockMessageQueueListenerFactory = new Mock<MessageQueueListenerFactory>();
            MockMessageQueueListenerFactory.Setup(x => x.CreateRabbitMQListener())
                .Returns<IMessageQueueListener>(null);
            Subject.MessageQueueListenerFactory = MockMessageQueueListenerFactory.Object;
        }

        [Test]
        public void ShouldStartListener()
        {
            Subject.ListenerTaskCount = 1;

            Subject.StartListener();

            MockMessageQueueListenerFactory.Verify(x => x.CreateRabbitMQListener(), Times.Once);
        }

        [Test]
        public void ShouldStartMultipleListeners()
        {
            var listenerTaskCount = 3;
            Subject.ListenerTaskCount = listenerTaskCount;

            Subject.StartListener();

            MockMessageQueueListenerFactory.Verify(x => x.CreateRabbitMQListener(), 
                Times.Exactly(listenerTaskCount));
        }

        [Test]
        public void ShouldSetCancellationTokenOnDispose()
        {
            Subject.CancellationTokenSource = new CancellationTokenSource();

            Subject.Dispose();

            Assert.That(Subject.CancellationTokenSource.IsCancellationRequested, Is.True);
        }
    }
}

