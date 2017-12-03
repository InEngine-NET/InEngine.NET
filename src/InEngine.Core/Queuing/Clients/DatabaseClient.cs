using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
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
                context.Messages.Add(new CommandEnvelopeModel() {
                    Status = CommandEnvelopeStatus.Pending,
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

            ICommandEnvelope commandEnvelope;
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
                                                      .FirstOrDefault(x => x.Status == CommandEnvelopeStatus.Pending&& x.QueueName == QueueName);
                            messageModel.Status = CommandEnvelopeStatus.InProgress;
                            context.SaveChanges();
                            commandEnvelope = messageModel;
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
                Queue.ExtractCommandInstanceFromMessageAndRun(commandEnvelope);
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
                                var messageModel = context.Messages.FirstOrDefault(x => x.Id == commandEnvelope.Id);
                                messageModel.Status = CommandEnvelopeStatus.Failed;
                                context.SaveChanges();
                                commandEnvelope = messageModel;
                            }

                            transaction.Commit();
                        }
                        catch (Exception dbException)
                        {
                            transaction.Rollback();
                            throw new CommandFailedException("Failed to set queue commandEnvelope from in-progress to failed.", dbException);
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
                                var messageModel = context.Messages.FirstOrDefault(x => x.Id == commandEnvelope.Id);
                                messageModel.Status = CommandEnvelopeStatus.Completed;
                                context.SaveChanges();
                                commandEnvelope = messageModel;
                            }
                            transaction.Commit();
                        }
                        catch (Exception dbException)
                        {
                            transaction.Rollback();
                            throw new CommandFailedException("Failed to set queue commandEnvelope from in-progress to completed.", dbException);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                throw new CommandFailedException($"Failed to remove completed commandEnvelope from queue", exception);
            }

            return true;
        }

        public long GetPendingQueueLength()
        {
            return GetQueueLength(CommandEnvelopeStatus.Pending);
        }

        public long GetInProgressQueueLength()
        {
            return GetQueueLength(CommandEnvelopeStatus.InProgress);
        }

        public long GetFailedQueueLength()
        {
            return GetQueueLength(CommandEnvelopeStatus.Failed);
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
            ClearQueue(CommandEnvelopeStatus.Pending);
            return true;
        }

        public bool ClearInProgressQueue()
        {
            ClearQueue(CommandEnvelopeStatus.InProgress);
            return true;
        }

        public bool ClearFailedQueue()
        {
            ClearQueue(CommandEnvelopeStatus.Failed);
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
                                                       .Where(x => x.QueueName == QueueName && x.Status == CommandEnvelopeStatus.Pending)
                                                        .OrderBy(x => x.QueuedAt);
                            foreach(var messageModel in messageModels)
                                messageModel.Status = CommandEnvelopeStatus.InProgress;
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

        public List<ICommandEnvelope> PeekPendingMessages(long from, long to)
        {
            return GetMessages(CommandEnvelopeStatus.Pending, from, to);
        }

        public List<ICommandEnvelope> PeekInProgressMessages(long from, long to)
        {
            return GetMessages(CommandEnvelopeStatus.InProgress, from, to);
        }

        public List<ICommandEnvelope> PeekFailedMessages(long from, long to)
        {
            return GetMessages(CommandEnvelopeStatus.Failed, from, to);
        }

        public List<ICommandEnvelope> GetMessages(string status, long from, long to)
        {
            using (var context = new QueueDbContext(QueueBaseName))
            {
                return context.Messages
                              .Where(x => x.QueueName == QueueName && x.Status == status)
                              .Skip(Convert.ToInt32(from))
                              .Take(Convert.ToInt32(to - from))
                              .Select(x => x as ICommandEnvelope)
                              .ToList();
            }
        }
    }
}
