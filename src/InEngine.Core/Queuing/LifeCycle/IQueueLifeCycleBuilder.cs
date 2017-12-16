using System;

namespace InEngine.Core.Queuing.LifeCycle
{
    public interface IQueueLifeCycleBuilder : IDispatch
    {
        AbstractCommand Command { get; set; }
        IQueueLifeCycleBuilder PingBefore(string url);
        IQueueLifeCycleBuilder PingAfter(string url);
        IQueueLifeCycleBuilder WriteOutputTo(string output);
        IQueueLifeCycleBuilder AppendOutputTo(string output);
        IQueueLifeCycleBuilder EmailOutputTo(string emailAddress);
        IQueueLifeCycleBuilder WithRetries(int maximumRetries);
    }
}
