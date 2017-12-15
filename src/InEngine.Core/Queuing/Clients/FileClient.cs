using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Common.Logging;
using InEngine.Core.Exceptions;
using InEngine.Core.Queuing.Message;

namespace InEngine.Core.Queuing.Clients
{
    public class FileClient : IQueueClient
    {
        static Mutex consumeLock = new Mutex();
        public static FileClientSettings ClientSettings { get; set; }

        public ILog Log { get; set; } = LogManager.GetLogger<FileClient>();
        public int Id { get; set; } = 0;
        public string QueueBaseName { get; set; }
        public string QueueName { get; set; }
        public bool UseCompression { get; set; }
        public string QueuePath { get { return Path.Combine(ClientSettings.BasePath, $"{QueueBaseName}_{QueueName}"); } }
        public string PendingQueuePath { 
            get { 
                var path = $"{QueuePath}_Pending";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            } 
        }

        public string InProgressQueuePath
        {
            get
            {
                var path = $"{QueuePath}_InProgress";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }
        public string FailedQueuePath
        {
            get
            {
                var path = $"{QueuePath}_Failed";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }

        public void Publish(AbstractCommand command)
        {
            if (!Directory.Exists(PendingQueuePath))
                Directory.CreateDirectory(PendingQueuePath);
            var serializedMessage = new CommandEnvelope() {
                IsCompressed = UseCompression,
                CommandClassName = command.GetType().FullName,
                PluginName = command.GetType().Assembly.GetName().Name,
                SerializedCommand = command.SerializeToJson(UseCompression)
            }.SerializeToJson();
            using (var streamWriter = File.CreateText(Path.Combine(PendingQueuePath, Guid.NewGuid().ToString()))) 
            {
                streamWriter.Write(serializedMessage);
            }   
        }

        public void Consume(CancellationToken cancellationToken)
        {
            try
            {
                while(true)
                {
                    if (Consume() == null)
                        Thread.Sleep(5000);   
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
            catch (OperationCanceledException exception)
            {
                Log.Debug(exception);
                return;
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        public ICommandEnvelope Consume()
        {
            FileInfo fileInfo;
            var inProgressFilePath = String.Empty;

            consumeLock.WaitOne();
            fileInfo = new DirectoryInfo(PendingQueuePath)
                .GetFiles()
                .OrderBy(x => x.LastWriteTimeUtc)
                .FirstOrDefault();
            if (fileInfo != null) {
                inProgressFilePath = Path.Combine(InProgressQueuePath, fileInfo.Name);
                fileInfo.MoveTo(inProgressFilePath);
            }
            if (fileInfo == null) {
                consumeLock.ReleaseMutex();    
                return null;
            }
            consumeLock.ReleaseMutex();
            
            var commandEnvelope = File.ReadAllText(inProgressFilePath).DeserializeFromJson<CommandEnvelope>() as ICommandEnvelope;
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
                    File.Move(inProgressFilePath, Path.Combine(PendingQueuePath, fileInfo.Name));
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
                Log.Error(exception);
                throw new CommandFailedException("Failed to move command from in-progress queue.", exception);
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
        {}

        public bool ClearFailedQueue()
        {
            return ClearQueue(FailedQueuePath);
        }

        public bool ClearInProgressQueue()
        {
            return ClearQueue(InProgressQueuePath);
        }

        public bool ClearPendingQueue()
        {
            return ClearQueue(PendingQueuePath);
        }

        public bool ClearQueue(string queuePath)
        {
            new DirectoryInfo(queuePath)
                .GetFiles()
                .ToList()
                .ForEach(x => x.Delete());
            return true;
        }

        public List<ICommandEnvelope> PeekFailedMessages(long from, long to)
        {
            return PeekMessages(FailedQueuePath, from, to);
        }

        public List<ICommandEnvelope> PeekInProgressMessages(long from, long to)
        {
            return PeekMessages(InProgressQueuePath, from, to);
        }

        public List<ICommandEnvelope> PeekPendingMessages(long from, long to)
        {
            return PeekMessages(PendingQueuePath, from, to);
        }

        public List<ICommandEnvelope> PeekMessages(string queuePath, long from, long to)
        {
            var maxResults = Convert.ToInt32(to + from);
            var files = new DirectoryInfo(queuePath)
                .GetFiles()
                .OrderBy(x => x.LastWriteTimeUtc)
                .ToList();

            if (files.Count() <= maxResults)
                return files.Select(x => File.ReadAllText(x.FullName).DeserializeFromJson<CommandEnvelope>() as ICommandEnvelope).ToList();

            return files
                .GetRange(Convert.ToInt32(from), Convert.ToInt32(to))
                .Select(x => File.ReadAllText(x.FullName).DeserializeFromJson<CommandEnvelope>() as ICommandEnvelope)
                .ToList();
        }

        public Dictionary<string, long> GetQueueLengths()
        {
            return new Dictionary<string, long>() {
                {"Pending", new DirectoryInfo(PendingQueuePath).GetFiles().LongCount()},
                {"In-progress", new DirectoryInfo(InProgressQueuePath).GetFiles().LongCount()},
                {"Failed", new DirectoryInfo(FailedQueuePath).GetFiles().LongCount()}
            };
        }
    }
}
