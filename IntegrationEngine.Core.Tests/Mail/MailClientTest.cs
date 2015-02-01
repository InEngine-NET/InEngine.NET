using BeekmanLabs.UnitTesting;
using Common.Logging;
using IntegrationEngine.Core.Configuration;
using IntegrationEngine.Core.Mail;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Net.Mail;
using System.Net.Sockets;

namespace IntegrationEngine.Core.Tests.Mail
{
    public class MailClientTest : TestBase<MailClient>
    {
        public Mock<ITcpClient> MockTcpClient { get; set; }
        public Mock<ILog> MockLog { get; set; }

        [SetUp]
        public void Setup()
        {
            MockLog = new Mock<ILog>();
            Subject.Log = MockLog.Object;
            Subject.MailConfiguration = new MailConfiguration() {
                HostName = "hostName",
                Port = 0,
            };
            MockTcpClient = new Mock<ITcpClient>();
            Subject.TcpClient = MockTcpClient.Object;
        }

        [Test]
        public void ShouldLogExceptionAndReturnFalseIfMailServerIsNotAvailable()
        {
            MockTcpClient.Setup(x => x.Connect(
                Subject.MailConfiguration.HostName,
                Subject.MailConfiguration.Port))
                .Throws(new SocketException());
            MockLog.Setup(x => x.Error(It.IsAny<SocketException>()));

            var actual = Subject.IsServerAvailable();
           
            Assert.That(actual, Is.False);
            MockLog.Verify(x => x.Error(It.IsAny<SocketException>()));
        }

        [Test]
        public void ShouldReturnTrueIfMailServerIsAvailable()
        {
            var expectedText = "Mail server status: Available";
            MockLog.Setup(x => x.Debug(expectedText));
            MockTcpClient.Setup(x => x.Connect(
                Subject.MailConfiguration.HostName,
                Subject.MailConfiguration.Port));
            var stream = new MemoryStream();
            var responseInBytes = System.Text.Encoding.UTF8.GetBytes("OK");
            stream.Write(responseInBytes, 0, responseInBytes.Length);
            MockTcpClient.Setup(x => x.GetStream()).Returns(stream);
            MockTcpClient.Setup(x => x.Close());

            var actual = Subject.IsServerAvailable();

            Assert.That(actual, Is.True);
            MockTcpClient.Verify(x => x.Close(), Times.Once);
        }

        [Test]
        public void ShouldSendMailMessage()
        {
            var expected = new MailMessage();
            var smtpClient = new Mock<ISmtpClient>();
            smtpClient.Setup(x => x.Send(expected));
            Subject.SmtpClient = smtpClient.Object;

            Subject.Send(expected);

            smtpClient.Verify(x => x.Send(expected), Times.Once);
        }

        [Test]
        public void ShouldLogExceptionIfMailMessageFailsToSend()
        {
            var expected = new MailMessage();
            var smtpClient = new Mock<ISmtpClient>();
            var expectedException = new Exception();
            smtpClient.Setup(x => x.Send(expected))
                .Throws(expectedException);
            Subject.SmtpClient = smtpClient.Object;

            Subject.Send(expected);

            smtpClient.Verify(x => x.Send(expected), Times.Once);
            MockLog.Verify(x => x.Error(expectedException), Times.Once);
        }
    }
}

