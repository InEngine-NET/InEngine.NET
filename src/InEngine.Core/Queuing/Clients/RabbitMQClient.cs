﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InEngine.Core.Exceptions;
using InEngine.Core.IO;
using InEngine.Core.Queuing.Message;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Extensions.Logging;

namespace InEngine.Core.Queuing.Clients;

public class RabbitMqClient : IQueueClient
{
    public static RabbitMQClientSettings ClientSettings { get; set; }
    public MailSettings MailSettings { get; set; }

    public ILogger Log { get; set; } = LogManager.GetLogger<SyncClient>();
    public int Id { get; set; } = 0;
    public string QueueBaseName { get; set; } = "InEngineQueue";
    public string QueueName { get; set; } = QueueNames.Primary;
    public string PendingQueueName => QueueBaseName + $":{QueueName}:{QueueNames.Pending}";
    public string FailedQueueName => QueueBaseName + $":{QueueName}:{QueueNames.Failed}";
    public bool UseCompression { get; set; }
    public string DeadLetterExchangeName => QueueBaseName + $":{QueueNames.DeadLetter}";
    public string ExchangeName => QueueBaseName;
    public string RoutingKey => QueueName;
    private IConnection connection;

    public IConnection Connection
    {
        get
        {
            if (connection != null)
                return connection;
            var factory = new ConnectionFactory()
            {
                HostName = ClientSettings.Host,
                Port = ClientSettings.Port,
                AutomaticRecoveryEnabled = true
            };
            if (!string.IsNullOrWhiteSpace(ClientSettings.Username) &&
                !string.IsNullOrWhiteSpace(ClientSettings.Password))
            {
                factory.UserName = ClientSettings.Username;
                factory.Password = ClientSettings.Password;
            }

            connection = factory.CreateConnection();
            return connection;
        }
    }

    private IModel channel;
    private bool isDisposed;

    public IModel Channel => channel ??= Connection.CreateModel();

    public void InitChannel()
    {
        Channel.ExchangeDeclare(QueueBaseName, ExchangeType.Direct);
        Channel.ExchangeDeclare(DeadLetterExchangeName, ExchangeType.Direct);
        Channel.QueueDeclare(PendingQueueName, true, false, false, new Dictionary<string, object>
        {
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
    {
    }

    public async Task Consume(CancellationToken cancellationToken)
    {
        InitChannel();
        var consumer = new EventingBasicConsumer(Channel);
        consumer.Received += async (model, result) =>
        {
            var eventingConsumer = (EventingBasicConsumer)model;
            if (eventingConsumer == null)
            {
                Log.LogWarning("EventingBasicConsumer is null while attempting to consume messages");
                return;
            }

            var serializedMessage = Encoding.UTF8.GetString(result.Body);
            var commandEnvelope = serializedMessage.DeserializeFromJson<CommandEnvelope>();
            if (commandEnvelope == null)
                throw new CommandFailedException("Could not deserialize the command.");

            var command = commandEnvelope.GetCommandInstanceAndIncrementRetry(() =>
            {
                eventingConsumer.Model.BasicNack(result.DeliveryTag, false, false);
            });

            try
            {
                command.WriteSummaryToConsole();
                await command.RunWithLifeCycleAsync();
            }
            catch (Exception exception)
            {
                Log.LogError(exception, "Error running task");
                if (command.CommandLifeCycle.ShouldRetry())
                    eventingConsumer.Model.BasicNack(result.DeliveryTag, false, true);
                else
                {
                    eventingConsumer.Model.BasicNack(result.DeliveryTag, false, false);
                    throw new CommandFailedException("Failed to run consumed command.", exception);
                }
            }

            Log.LogDebug("Acknowledging message...");
            eventingConsumer.Model.BasicAck(result.DeliveryTag, false);
        };
        Channel.BasicConsume(queue: PendingQueueName, autoAck: false, consumer: consumer);

        await Task.Yield();
    }

    public async Task<ICommandEnvelope> Consume()
    {
        InitChannel();
        var result = Channel.BasicGet(PendingQueueName, false);
        if (result == null)
            return null;

        var serializedMessage = Encoding.UTF8.GetString(result.Body);
        var commandEnvelope = serializedMessage.DeserializeFromJson<CommandEnvelope>();
        if (commandEnvelope == null)
            throw new CommandFailedException("Could not deserialize the command.");

        var command = commandEnvelope.GetCommandInstanceAndIncrementRetry(() =>
        {
            Channel.BasicNack(result.DeliveryTag, false, false);
        });

        try
        {
            command.WriteSummaryToConsole();
            await command.RunWithLifeCycleAsync();
        }
        catch (Exception exception)
        {
            Log.LogError(exception, "Error running task");
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

    public bool ClearInProgressQueue() => throw new NotImplementedException();
    public List<ICommandEnvelope> PeekFailedMessages(long from, long to) => throw new NotImplementedException();
    public List<ICommandEnvelope> PeekInProgressMessages(long from, long to) => throw new NotImplementedException();
    public List<ICommandEnvelope> PeekPendingMessages(long from, long to) => throw new NotImplementedException();
    public void RepublishFailedMessages() => throw new NotImplementedException();

    #endregion
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (isDisposed) 
            return;
        if (!disposing) 
            return;
        if (Connection is { IsOpen: true })
            Connection.Close();
        Connection.Dispose();
        isDisposed = true;
    }

    public Dictionary<string, long> GetQueueLengths()
    {
        InitChannel();
        return new Dictionary<string, long>()
        {
            { QueueNames.Pending, Channel.MessageCount(PendingQueueName) },
            { QueueNames.Failed, Channel.MessageCount(FailedQueueName) }
        };
    }
}