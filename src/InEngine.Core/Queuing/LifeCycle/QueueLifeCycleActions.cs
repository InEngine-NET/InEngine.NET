using System;

namespace InEngine.Core.Queuing.LifeCycle
{
    public class QueueLifeCycleActions : IQueueLifeCycleActions
    {
        public AbstractCommand Command { get; set; }
        public QueueAdapter QueueAdapter { get; set; }

        public QueueLifeCycleActions()
        {}

        public QueueLifeCycleActions(AbstractCommand command) : this()
        {
            Command = command;
        }

        public IQueueLifeCycleActions Before(Action<AbstractCommand> action)
        {
            Command.CommandLifeCycle.BeforeAction = action;
            return this;
        }

        public IQueueLifeCycleActions After(Action<AbstractCommand> action)
        {
            Command.CommandLifeCycle.AfterAction = action;
            return this;
        }

        public IQueueLifeCycleActions PingBefore(string url)
        {
            Command.CommandLifeCycle.ShouldPingBefore = true;
            Command.CommandLifeCycle.PingBeforeUrl = url;
            return this;
        }

        public IQueueLifeCycleActions PingAfter(string url)
        {
            Command.CommandLifeCycle.ShouldPingAfter = true;
            Command.CommandLifeCycle.PingAfterUrl = url;
            return this;
        }

        public IQueueLifeCycleActions WriteOutputTo(string output)
        {
            Command.CommandLifeCycle.ShouldWriteOutputToFile = true;
            Command.CommandLifeCycle.WriteOutputToFilePath = output;
            return this;
        }

        public IQueueLifeCycleActions AppendOutputTo(string output)
        {
            Command.CommandLifeCycle.ShouldAppendOutputToFile = true;
            Command.CommandLifeCycle.AppendOutputToFilePath = output;
            return this;
        }

        public IQueueLifeCycleActions EmailOutputTo(string emailAddress)
        {
            Command.CommandLifeCycle.ShouldEmailOutput = true;
            Command.CommandLifeCycle.EmailOutputToAddress = emailAddress;
            return this;
        }

        public IQueueLifeCycleActions ToPrimaryQueue()
        {
            QueueAdapter = QueueAdapter.Make(false);
            return this;
        }

        public IQueueLifeCycleActions ToSecondaryQueue()
        {
            QueueAdapter = QueueAdapter.Make(true);
            return this;
        }

        public IQueueLifeCycleActions WithRetries(int maximumRetries)
        {
            Command.CommandLifeCycle.MaximumRetries = maximumRetries;
            return this;
        }

        public void Dispatch()
        {
            if (QueueAdapter == null)
                QueueAdapter = QueueAdapter.Make();
            QueueAdapter.Publish(Command);
        }
    }
}
