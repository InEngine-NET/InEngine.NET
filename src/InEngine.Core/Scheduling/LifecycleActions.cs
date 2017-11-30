using System;

namespace InEngine.Core.Scheduling
{
    public class LifecycleActions
    {
        public JobRegistration JobRegistration { get; set; }


        public LifecycleActions Before(Action<AbstractCommand> action)
        {
            JobRegistration.Command.ExecutionLifeCycle.BeforeAction = action;
            return this;
        }

        public LifecycleActions After(Action<AbstractCommand> action)
        {
            JobRegistration.Command.ExecutionLifeCycle.AfterAction = action;
            return this;
        }

        public LifecycleActions PingBefore(string url)
        {
            JobRegistration.Command.ExecutionLifeCycle.ShouldPingBefore = true;
            JobRegistration.Command.ExecutionLifeCycle.PingBeforeUrl = url;
            return this;
        }

        public LifecycleActions PingAfter(string url)
        {
            JobRegistration.Command.ExecutionLifeCycle.ShouldPingAfter = true;
            JobRegistration.Command.ExecutionLifeCycle.PingAfterUrl = url;
            return this;
        }

        public LifecycleActions WriteOutputTo(string output)
        {
            JobRegistration.Command.ExecutionLifeCycle.ShouldWriteOutputToFile = true;
            JobRegistration.Command.ExecutionLifeCycle.WriteOutputToFilePath = output;
            return this;
        }

        public LifecycleActions AppendOutputTo(string output)
        {
            JobRegistration.Command.ExecutionLifeCycle.ShouldAppendOutputToFile = true;
            JobRegistration.Command.ExecutionLifeCycle.AppendOutputToFilePath = output;
            return this;
        }

        public LifecycleActions EmailOutputTo(string emailAddress)
        {
            JobRegistration.Command.ExecutionLifeCycle.ShouldEmailOutput = true;
            JobRegistration.Command.ExecutionLifeCycle.EmailOutputToAddress = emailAddress;
            return this;
        }
    }
}
