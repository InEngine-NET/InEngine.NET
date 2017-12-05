using System;

namespace InEngine.Core.Queuing
{
    public interface IQueueLifeCycleActions : IDispatch
    {
        AbstractCommand Command { get; set; }
        IQueueLifeCycleActions PingBefore(string url);
        IQueueLifeCycleActions PingAfter(string url);
        IQueueLifeCycleActions WriteOutputTo(string output);
        IQueueLifeCycleActions AppendOutputTo(string output);
        IQueueLifeCycleActions EmailOutputTo(string emailAddress);
        IQueueLifeCycleActions WithRetries(int maximumRetries);
    }
}
