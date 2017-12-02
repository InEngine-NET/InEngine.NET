using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InEngine.Core.Commands;
using InEngine.Core.Exceptions;

namespace InEngine.Core.Queuing.Clients
{
    public class FileClient : IQueueClient
    {
        public string QueueBaseName { get; set; }
        public string QueueName { get; set; }
        public bool UseCompression { get; set; }
        public string QueuePath { get { return $"{QueueBaseName}_{QueueName}"; } }
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

        public void Publish(ICommand command)
        {
            if (!Directory.Exists(PendingQueuePath))
                Directory.CreateDirectory(PendingQueuePath);
            var serializedMessage = new Message()
            {
                IsCompressed = UseCompression,
                CommandClassName = command.GetType().FullName,
                CommandAssemblyName = command.GetType().Assembly.GetName().Name + ".dll",
                SerializedCommand = command.SerializeToJson(UseCompression)
            }.SerializeToJson();
            using (var streamWriter = File.CreateText(Path.Combine(PendingQueuePath, Guid.NewGuid().ToString()))) 
            {
                streamWriter.Write(serializedMessage);
            }   
        }

        public bool Consume()
        {
            var fileInfo = new DirectoryInfo(PendingQueuePath)
                .GetFiles()
                .OrderBy(x => x.LastWriteTimeUtc)
                .FirstOrDefault();
            if (fileInfo == null)
                return false;
            var inProgressFilePath = Path.Combine(InProgressQueuePath, fileInfo.Name);

            try 
            {
                fileInfo.MoveTo(inProgressFilePath);
            }
            catch (FileNotFoundException)
            {
                // Another process probably consumed the file when it was read and moved.
                return false;
            }

            var message = File.ReadAllText(inProgressFilePath).DeserializeFromJson<Message>();
            try
            {
                Queue.ExtractCommandInstanceFromMessage(message as IMessage).Run();
            }
            catch (Exception exception)
            {
                File.Move(inProgressFilePath, Path.Combine(FailedQueuePath, fileInfo.Name));
                throw new CommandFailedException("Failed to consume message.", exception);
            }

            try
            {
                File.Delete(inProgressFilePath);
            }
            catch (Exception exception)
            {
                throw new CommandFailedException("Failed to move message from in-progress queue.", exception);
            }
            return true;
        }

        public void RepublishFailedMessages()
        {
            new DirectoryInfo(FailedQueuePath)
                .GetFiles()
                .ToList()
                .ForEach(x => x.MoveTo(Path.Combine(PendingQueuePath, x.Name)));
        }

        public long GetFailedQueueLength()
        {
            return new DirectoryInfo(FailedQueuePath).GetFiles().LongCount();
        }

        public long GetInProgressQueueLength()
        {
            return new DirectoryInfo(InProgressQueuePath).GetFiles().LongCount();
        }

        public long GetPendingQueueLength()
        {
            return new DirectoryInfo(PendingQueuePath).GetFiles().LongCount();
        }

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

        public List<IMessage> PeekFailedMessages(long from, long to)
        {
            return PeekMessages(FailedQueuePath, from, to);
        }

        public List<IMessage> PeekInProgressMessages(long from, long to)
        {
            return PeekMessages(InProgressQueuePath, from, to);
        }

        public List<IMessage> PeekPendingMessages(long from, long to)
        {
            return PeekMessages(PendingQueuePath, from, to);
        }

        public List<IMessage> PeekMessages(string queuePath, long from, long to)
        {
            var maxResults = Convert.ToInt32(to + from);
            var files = new DirectoryInfo(queuePath)
                .GetFiles()
                .OrderBy(x => x.LastWriteTimeUtc)
                .ToList();

            if (files.Count() <= maxResults)
                return files.Select(x => File.ReadAllText(x.FullName).DeserializeFromJson<Message>() as IMessage).ToList();

            return files
                .GetRange(Convert.ToInt32(from), Convert.ToInt32(to))
                .Select(x => File.ReadAllText(x.FullName).DeserializeFromJson<Message>() as IMessage)
                .ToList();
        }
    }
}
