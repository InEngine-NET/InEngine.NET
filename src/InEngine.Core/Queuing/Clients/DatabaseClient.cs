using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using InEngine.Core.Commands;
using InEngine.Core.Exceptions;
using InEngine.Core.Queuing.Clients.Database;
using Newtonsoft.Json;

namespace InEngine.Core.Queuing.Clients
{
    public class DatabaseClient : IQueueClient
    {
        public string QueueBaseName { get; set; } = "InEngineQueue";
        public string QueueName { get; set; } = "Primary";
        public bool UseCompression { get; set; }

        public void Publish(ICommand command)
        {
            var serializedCommand = JsonConvert.SerializeObject(command);

            using (var context = new QueueDbContext(QueueBaseName))
            {
                context.Messages.Add(new MessageModel() {
                    Status = MessageStatus.Pending,
                    IsCompressed = UseCompression,
                    CommandClassName = command.GetType().FullName,
                    CommandAssemblyName = command.GetType().Assembly.GetName().Name + ".dll",
                    SerializedCommand = UseCompression ? serializedCommand.Compress() : serializedCommand
                });
                context.SaveChanges();
            }
        }

        public bool Consume()
        {

            IMessage message;
            using (var conn = new SqlConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        using (var context = new QueueDbContext(conn, false, QueueBaseName))
                        {
                            context.Database.UseTransaction(transaction);
                            var messageModel = context.Messages
                                                      .OrderBy(x => x.QueuedAt)
                                                      .FirstOrDefault(x => x.Status == MessageStatus.Pending&& x.QueueName == QueueName);
                            messageModel.Status = MessageStatus.InProgress;
                            context.SaveChanges();
                            message = messageModel;
                        }

                        transaction.Commit();
                    }
                    catch (Exception exception)
                    {
                        transaction.Rollback();
                        throw new CommandFailedException("Consuming from queue failed.", exception);
                    }
                }
            }

            try
            {
                Queue.ExtractCommandInstanceFromMessage(message).Run();
            }
            catch (Exception exception)
            {
                using (var conn = new SqlConnection())
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction(IsolationLevel.Serializable))
                    {
                        try
                        {
                            using (var context = new QueueDbContext(conn, false, QueueBaseName))
                            {
                                context.Database.UseTransaction(transaction);
                                var messageModel = context.Messages.FirstOrDefault(x => x.Id == message.Id);
                                messageModel.Status = MessageStatus.Failed;
                                context.SaveChanges();
                                message = messageModel;
                            }

                            transaction.Commit();
                        }
                        catch (Exception dbException)
                        {
                            transaction.Rollback();
                            throw new CommandFailedException("Failed to set queue message from in-progress to failed.", dbException);
                        }
                    }
                }
                throw new CommandFailedException("Consumed command failed.", exception);
            }

            try
            {
                using (var conn = new SqlConnection())
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction(IsolationLevel.Serializable))
                    {
                        try
                        {
                            using (var context = new QueueDbContext(conn, false, QueueBaseName))
                            {
                                context.Database.UseTransaction(transaction);
                                var messageModel = context.Messages.FirstOrDefault(x => x.Id == message.Id);
                                messageModel.Status = MessageStatus.Completed;
                                context.SaveChanges();
                                message = messageModel;
                            }
                            transaction.Commit();
                        }
                        catch (Exception dbException)
                        {
                            transaction.Rollback();
                            throw new CommandFailedException("Failed to set queue message from in-progress to completed.", dbException);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                throw new CommandFailedException($"Failed to remove completed message from queue", exception);
            }

            return true;
        }

        public long GetPendingQueueLength()
        {
            return GetQueueLength(MessageStatus.Pending);
        }

        public long GetInProgressQueueLength()
        {
            return GetQueueLength(MessageStatus.InProgress);
        }

        public long GetFailedQueueLength()
        {
            return GetQueueLength(MessageStatus.Failed);
        }

        public long GetQueueLength(string status)
        {
            using (var context = new QueueDbContext(QueueBaseName))
            {
                return context.Messages.Where(x => x.QueueName == QueueName && x.Status == status).LongCount();
            }
        }

        public bool ClearPendingQueue()
        {
            ClearQueue(MessageStatus.Pending);
            return true;
        }

        public bool ClearInProgressQueue()
        {
            ClearQueue(MessageStatus.InProgress);
            return true;
        }

        public bool ClearFailedQueue()
        {
            ClearQueue(MessageStatus.Failed);
            return true;
        }

        public bool ClearQueue(string status)
        {
            using (var context = new QueueDbContext(QueueBaseName))
            {
                context.Messages
                       .RemoveRange(context.Messages.Where(x => x.QueueName == QueueName && x.Status == status));
                context.SaveChanges();
            }
            return true;
        }

        public void RepublishFailedMessages()
        {
            using (var conn = new SqlConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        using (var context = new QueueDbContext(conn, false, QueueBaseName))
                        {
                            context.Database.UseTransaction(transaction);
                            var messageModels = context.Messages
                                                       .Where(x => x.QueueName == QueueName && x.Status == MessageStatus.Pending)
                                                        .OrderBy(x => x.QueuedAt);
                            foreach(var messageModel in messageModels)
                                messageModel.Status = MessageStatus.InProgress;
                            context.SaveChanges();
                        }

                        transaction.Commit();
                    }
                    catch (Exception exception)
                    {
                        transaction.Rollback();
                        throw new CommandFailedException("Consuming from queue failed.", exception);
                    }
                }
            }
        }

        public List<IMessage> PeekPendingMessages(long from, long to)
        {
            return GetMessages(MessageStatus.Pending, from, to);
        }

        public List<IMessage> PeekInProgressMessages(long from, long to)
        {
            return GetMessages(MessageStatus.InProgress, from, to);
        }

        public List<IMessage> PeekFailedMessages(long from, long to)
        {
            return GetMessages(MessageStatus.Failed, from, to);
        }

        public List<IMessage> GetMessages(string status, long from, long to)
        {
            using (var context = new QueueDbContext(QueueBaseName))
            {
                return context.Messages
                              .Where(x => x.QueueName == QueueName && x.Status == status)
                              .Skip(Convert.ToInt32(from))
                              .Take(Convert.ToInt32(to - from))
                              .Select(x => x as IMessage)
                              .ToList();
            }
        }
    }
}
