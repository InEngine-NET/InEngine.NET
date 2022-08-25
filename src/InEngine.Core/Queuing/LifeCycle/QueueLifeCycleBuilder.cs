using System;
using InEngine.Core.IO;

namespace InEngine.Core.Queuing.LifeCycle
{
    public class QueueLifeCycleBuilder : IQueueLifeCycleBuilder, IHasMailSettings
    {
        public AbstractCommand Command { get; set; }
        public QueueAdapter QueueAdapter { get; set; }
        public QueueSettings QueueSettings { get; set; }
        public MailSettings MailSettings { get; set; }

        public QueueLifeCycleBuilder()
        {}

        public QueueLifeCycleBuilder(AbstractCommand command) : this()
        {
            Command = command;
        }

        public IQueueLifeCycleBuilder Before(Action<AbstractCommand> action)
        {
            Command.CommandLifeCycle.BeforeAction = action;
            return this;
        }

        public IQueueLifeCycleBuilder After(Action<AbstractCommand> action)
        {
            Command.CommandLifeCycle.AfterAction = action;
            return this;
        }

        public IQueueLifeCycleBuilder PingBefore(string url)
        {
            Command.CommandLifeCycle.ShouldPingBefore = true;
            Command.CommandLifeCycle.PingBeforeUrl = url;
            return this;
        }

        public IQueueLifeCycleBuilder PingAfter(string url)
        {
            Command.CommandLifeCycle.ShouldPingAfter = true;
            Command.CommandLifeCycle.PingAfterUrl = url;
            return this;
        }

        public IQueueLifeCycleBuilder WriteOutputTo(string output)
        {
            Command.CommandLifeCycle.ShouldWriteOutputToFile = true;
            Command.CommandLifeCycle.WriteOutputToFilePath = output;
            return this;
        }

        public IQueueLifeCycleBuilder AppendOutputTo(string output)
        {
            Command.CommandLifeCycle.ShouldAppendOutputToFile = true;
            Command.CommandLifeCycle.AppendOutputToFilePath = output;
            return this;
        }

        public IQueueLifeCycleBuilder EmailOutputTo(string emailAddress)
        {
            Command.CommandLifeCycle.ShouldEmailOutput = true;
            Command.CommandLifeCycle.EmailOutputToAddress = emailAddress;
            return this;
        }

        public IQueueLifeCycleBuilder ToPrimaryQueue()
        {
            QueueAdapter = QueueAdapter.Make(false, QueueSettings, MailSettings);
            return this;
        }

        public IQueueLifeCycleBuilder ToSecondaryQueue()
        {
            QueueAdapter = QueueAdapter.Make(true, QueueSettings, MailSettings);
            return this;
        }

        public IQueueLifeCycleBuilder WithRetries(int maximumRetries)
        {
            Command.CommandLifeCycle.MaximumRetries = maximumRetries;
            return this;
        }

        public void Dispatch()
        {
            if (QueueAdapter == null)
                QueueAdapter = QueueAdapter.Make(true, QueueSettings, MailSettings);
                
            QueueAdapter.Publish(Command);
        }
    }
}
