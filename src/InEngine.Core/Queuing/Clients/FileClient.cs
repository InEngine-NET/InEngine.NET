using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using InEngine.Core.Exceptions;
using InEngine.Core.IO;
using InEngine.Core.Queuing.Message;
using Microsoft.Extensions.Logging;

namespace InEngine.Core.Queuing.Clients;

public class FileClient : IQueueClient
{
    private static readonly Mutex ConsumeLock = new Mutex();
    public static FileClientSettings ClientSettings { get; set; }
    public MailSettings MailSettings { get; set; }

    public ILogger Log { get; set; } = LogManager.GetLogger<FileClient>();
    public int Id { get; set; } = 0;
    public string QueueBaseName { get; set; }
    public string QueueName { get; set; }
    public bool UseCompression { get; set; }

    public string QueuePath => Path.Combine(ClientSettings.BasePath, $"{QueueBaseName}_{QueueName}");

    public static string EnsureQueueExists(string path)
    {
        if (path != null && !Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }

    public string PendingQueuePath => EnsureQueueExists($"{QueuePath}_{QueueNames.Pending}");
    public string InProgressQueuePath => EnsureQueueExists($"{QueuePath}_{QueueNames.InProgress}");
    public string FailedQueuePath => EnsureQueueExists($"{QueuePath}_{QueueNames.Failed}");

    public void Publish(AbstractCommand command)
    {
        PublishToQueue(new CommandEnvelope()
        {
            IsCompressed = UseCompression,
            CommandClassName = command.GetType().FullName,
            PluginName = command.GetType().Assembly.GetName().Name,
            SerializedCommand = command.SerializeToJson(UseCompression)
        }, PendingQueuePath);
    }

    private void PublishToQueue(CommandEnvelope commandEnvelope, string queuePath)
    {
        EnsureQueueExists(queuePath);
        File.WriteAllText(
            Path.Combine(queuePath, Guid.NewGuid().ToString()),
            commandEnvelope.SerializeToJson()
        );
    }

    public void Consume(CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                try
                {
                    if (Consume() == null)
                        Thread.Sleep(5000);
                }
                catch (Exception exception)
                {
                    Log.LogError(exception, "There was an error while consuming a queue");
                }

                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        catch (OperationCanceledException exception)
        {
            Log.LogError(exception, "OperationCanceledException");
        }
        catch (Exception exception)
        {
            Log.LogError(exception, "Generic exception");
        }
    }

    public ICommandEnvelope Consume()
    {
        FileInfo fileInfo;
        var inProgressFilePath = string.Empty;

        ConsumeLock.WaitOne();
        fileInfo = new DirectoryInfo(PendingQueuePath)
            .GetFiles()
            .MinBy(x => x.LastWriteTimeUtc);
        if (fileInfo != null)
        {
            inProgressFilePath = Path.Combine(InProgressQueuePath, fileInfo.Name);
            fileInfo.MoveTo(inProgressFilePath);
        }

        if (fileInfo == null)
        {
            ConsumeLock.ReleaseMutex();
            return null;
        }

        ConsumeLock.ReleaseMutex();

        var commandEnvelope = File.ReadAllText(inProgressFilePath).DeserializeFromJson<CommandEnvelope>();
        var command = commandEnvelope.GetCommandInstanceAndIncrementRetry(() =>
        {
            File.Move(inProgressFilePath, Path.Combine(FailedQueuePath, fileInfo.Name));
        });

        try
        {
            command.WriteSummaryToConsole();
            command.RunWithLifeCycle();
        }
        catch (Exception exception)
        {
            Log.LogError(exception, "Exception while consuming a command");
            if (command.CommandLifeCycle.ShouldRetry())
            {
                PublishToQueue(commandEnvelope, PendingQueuePath);
                File.Delete(Path.Combine(PendingQueuePath, fileInfo.Name));
            }
            else
                File.Move(inProgressFilePath, Path.Combine(FailedQueuePath, fileInfo.Name));

            throw new CommandFailedException("Failed to consume command.", exception);
        }

        try
        {
            File.Delete(inProgressFilePath);
        }
        catch (Exception exception)
        {
            const string message = "Failed to move command from in-progress queue.";
            Log.LogError(exception, message);
            throw new CommandFailedException(message, exception);
        }

        return commandEnvelope;
    }

    public void RepublishFailedMessages()
    {
        new DirectoryInfo(FailedQueuePath)
            .GetFiles()
            .ToList()
            .ForEach(x => x.MoveTo(Path.Combine(PendingQueuePath, x.Name)));
    }

    public void Recover()
    {
    }

    public bool ClearFailedQueue() => ClearQueue(FailedQueuePath);
    public bool ClearInProgressQueue() => ClearQueue(InProgressQueuePath);
    public bool ClearPendingQueue() => ClearQueue(PendingQueuePath);

    public bool ClearQueue(string queuePath)
    {
        new DirectoryInfo(queuePath)
            .GetFiles()
            .ToList()
            .ForEach(x => x.Delete());
        return true;
    }

    public List<ICommandEnvelope> PeekFailedMessages(long from, long to) => PeekMessages(FailedQueuePath, from, to);

    public List<ICommandEnvelope> PeekInProgressMessages(long from, long to) =>
        PeekMessages(InProgressQueuePath, from, to);

    public List<ICommandEnvelope> PeekPendingMessages(long from, long to) => PeekMessages(PendingQueuePath, from, to);

    public List<ICommandEnvelope> PeekMessages(string queuePath, long from, long to)
    {
        var maxResults = Convert.ToInt32(to + from);
        var files = new DirectoryInfo(queuePath)
            .GetFiles()
            .OrderBy(x => x.LastWriteTimeUtc)
            .ToList();

        if (files.Count <= maxResults)
            return files.Select(x =>
                File.ReadAllText(x.FullName).DeserializeFromJson<CommandEnvelope>() as ICommandEnvelope).ToList();

        return files
            .GetRange(Convert.ToInt32(from), Convert.ToInt32(to))
            .Select(x => File.ReadAllText(x.FullName).DeserializeFromJson<CommandEnvelope>() as ICommandEnvelope)
            .ToList();
    }

    public Dictionary<string, long> GetQueueLengths()
    {
        return new Dictionary<string, long>()
        {
            { QueueNames.Pending, new DirectoryInfo(PendingQueuePath).GetFiles().Length },
            { QueueNames.InProgress, new DirectoryInfo(InProgressQueuePath).GetFiles().Length },
            { QueueNames.Failed, new DirectoryInfo(FailedQueuePath).GetFiles().Length }
        };
    }

    public void Dispose() => GC.SuppressFinalize(this);
}