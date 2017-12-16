using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Common.Logging;
using InEngine.Core.Exceptions;
using InEngine.Core.Queuing.Message;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InEngine.Core.Queuing.Clients
{
    public class RabbitMQClient : IQueueClient, IDisposable
    {
        public static RabbitMQClientSettings ClientSettings { get; set; } 

        public ILog Log { get; set; } = LogManager.GetLogger<SyncClient>();
        public int Id { get; set; } = 0;
        public string QueueBaseName { get; set; } = "InEngineQueue";
        public string QueueName { get; set; } = "Primary";
        public string PendingQueueName { get { return QueueBaseName + $":{QueueName}:Pending"; } }
        public string FailedQueueName { get { return QueueBaseName + $":{QueueName}:Failed"; } }
        public bool UseCompression { get; set; }
        public string DeadLetterExchangeName { get { return QueueBaseName + $":DeadLetter"; } }
        public string ExchangeName { get { return QueueBaseName; } }
        public string RoutingKey { get { return QueueName; } }
        IConnection _connection;
        public IConnection Connection { get {
                if (_connection == null) {
                    var factory = new ConnectionFactory() {
                        HostName = ClientSettings.Host,
                        Port = ClientSettings.Port,
                        AutomaticRecoveryEnabled = true
                    };
                    if (!string.IsNullOrWhiteSpace(ClientSettings.Username) && 
                        !string.IsNullOrWhiteSpace(ClientSettings.Password)) {
                        factory.UserName = ClientSettings.Username;
                        factory.Password = ClientSettings.Password;
                    }
                    _connection = factory.CreateConnection();
                }
                return _connection; 
            } 
        }
        IModel _channel;
        public IModel Channel {
            get {
                if (_channel == null)
                    _channel = Connection.CreateModel();
                return _channel;
            }
        }

        public void InitChannel()
        {
            Channel.ExchangeDeclare(QueueBaseName, ExchangeType.Direct);
            Channel.ExchangeDeclare(DeadLetterExchangeName, ExchangeType.Direct);
            Channel.QueueDeclare(PendingQueueName, true, false, false, new Dictionary<string, object> {
                { "x-dead-letter-exchange", DeadLetterExchangeName }
            });
            Channel.QueueBind(PendingQueueName, ExchangeName, RoutingKey, null);
            Channel.QueueDeclare(FailedQueueName, true, false, false);
            Channel.QueueBind(FailedQueueName, DeadLetterExchangeName, RoutingKey, null);
        }

        public void Publish(AbstractCommand command)
        {
            InitChannel();
            var body = Encoding.UTF8.GetBytes(new CommandEnvelope()
            {
                IsCompressed = UseCompression,
                CommandClassName = command.GetType().FullName,
                PluginName = command.GetType().Assembly.GetName().Name,
                SerializedCommand = command.SerializeToJson(UseCompression)
            }.SerializeToJson());

            var properties = Channel.CreateBasicProperties();
            properties.Persistent = true;
            Channel.BasicPublish(exchange: ExchangeName,
                                 routingKey: RoutingKey,
                                 basicProperties: properties,
                                 mandatory: true,
                                 body: body);
        }

        public void Recover()
        { }

        public void Consume(CancellationToken cancellationToken)
        {
            InitChannel();
            var consumer = new EventingBasicConsumer(Channel);
            consumer.Received += (model, result) => {
                var eventingConsumer = (EventingBasicConsumer)model;

                var serializedMessage = Encoding.UTF8.GetString(result.Body);
                var commandEnvelope = serializedMessage.DeserializeFromJson<CommandEnvelope>();
                if (commandEnvelope == null)
                    throw new CommandFailedException("Could not deserialize the command.");

                var command = commandEnvelope.GetCommandInstance();
                command.CommandLifeCycle.IncrementRetry();
                commandEnvelope.SerializedCommand = command.SerializeToJson(UseCompression);
                try
                {
                    command.WriteSummaryToConsole();
                    command.RunWithLifeCycle();
                }
                catch (Exception exception)
                {
                    Log.Error(exception);
                    if (command.CommandLifeCycle.ShouldRetry())
                        eventingConsumer.Model.BasicNack(result.DeliveryTag, false, true);
                    else
                    {
                        eventingConsumer.Model.BasicNack(result.DeliveryTag, false, false);
                        throw new CommandFailedException("Failed to run consumed command.", exception);
                    }
                }
                Log.Debug("Acknowledging message...");
                eventingConsumer.Model.BasicAck(result.DeliveryTag, false);
            };
            Channel.BasicConsume(queue: PendingQueueName, autoAck: false, consumer: consumer);
        }

        public ICommandEnvelope Consume()
        {
            InitChannel();
            var result = Channel.BasicGet(PendingQueueName, false);
            if (result == null)
                return null;

            var serializedMessage = Encoding.UTF8.GetString(result.Body);
            var commandEnvelope = serializedMessage.DeserializeFromJson<CommandEnvelope>();
            if (commandEnvelope == null)
                throw new CommandFailedException("Could not deserialize the command.");

            var command = commandEnvelope.GetCommandInstance();
            command.CommandLifeCycle.IncrementRetry();
            commandEnvelope.SerializedCommand = command.SerializeToJson(UseCompression);
            try
            {
                command.WriteSummaryToConsole();
                command.RunWithLifeCycle();
            }
            catch (Exception exception)
            {
                Log.Error(exception);
                if (command.CommandLifeCycle.ShouldRetry())
                    Channel.BasicNack(result.DeliveryTag, false, true);
                else
                {
                    Channel.BasicNack(result.DeliveryTag, false, false);
                    throw new CommandFailedException("Failed to run consumed command.", exception);
                }
            }
            Channel.BasicAck(result.DeliveryTag, false);
            return commandEnvelope;
        }

        public bool ClearPendingQueue()
        {
            InitChannel();
            return Channel.QueuePurge(PendingQueueName) > 0;
        }

        public bool ClearFailedQueue()
        {
            InitChannel();
            return Channel.QueuePurge(FailedQueueName) > 0;
        }

        #region Not implemented
        public bool ClearInProgressQueue()
        {
            throw new NotImplementedException();
        }

        public List<ICommandEnvelope> PeekFailedMessages(long from, long to)
        {
            throw new NotImplementedException();
        }

        public List<ICommandEnvelope> PeekInProgressMessages(long from, long to)
        {
            throw new NotImplementedException();
        }

        public List<ICommandEnvelope> PeekPendingMessages(long from, long to)
        {
            throw new NotImplementedException();
        }

        public void RepublishFailedMessages()
        {
            throw new NotImplementedException();
        }
        #endregion

        public void Dispose()
        {
            if (Connection != null && Connection.IsOpen)
                Connection.Close();
        }

        public Dictionary<string, long> GetQueueLengths()
        {
            InitChannel();
            return new Dictionary<string, long>() {
                {"Pending", Channel.MessageCount(PendingQueueName)},
                {"Failed", Channel.MessageCount(FailedQueueName)}
            };
        }
    }
}
