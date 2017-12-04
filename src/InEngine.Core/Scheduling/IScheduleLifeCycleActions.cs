using System;

namespace InEngine.Core.Scheduling
{
    public interface IScheduleLifeCycleActions
    {
        AbstractCommand Command { get; set; }
        IScheduleLifeCycleActions Before(Action<AbstractCommand> action);
        IScheduleLifeCycleActions After(Action<AbstractCommand> action);
        IScheduleLifeCycleActions PingBefore(string url);
        IScheduleLifeCycleActions PingAfter(string url);
        IScheduleLifeCycleActions WriteOutputTo(string output);
        IScheduleLifeCycleActions AppendOutputTo(string output);
        IScheduleLifeCycleActions EmailOutputTo(string emailAddress);
    }
}
