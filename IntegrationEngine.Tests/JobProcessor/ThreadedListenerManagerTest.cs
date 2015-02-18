using BeekmanLabs.UnitTesting;
using Common.Logging;
using IntegrationEngine.JobProcessor;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;

namespace IntegrationEngine.Tests.JobProcessor
{
    public class ThreadedListenerManagerTest : TestBase<ThreadedListenerManager>
    {
        [Test]
        public void ShouldSpawnAThread()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            Subject.CancellationTokenSource = cancellationTokenSource;
            var mockMessageQueueListener = new Mock<IMessageQueueListener>();
            Subject.MessageQueueListener = mockMessageQueueListener.Object;
            mockMessageQueueListener.Setup(x => x.Listen(cancellationTokenSource.Token));
            var mockLog = new Mock<ILog>();
            Subject.Log = mockLog.Object;
            var listenerStartMessage = "Message queue listener started.";
            mockLog.Setup(x => x.Info(listenerStartMessage));

            Subject.StartListener();

            mockMessageQueueListener.Verify(x => x.Listen(cancellationTokenSource.Token), Times.Once);
            mockLog.Verify(x => x.Info(listenerStartMessage), Times.Once);
        }
    }
}

