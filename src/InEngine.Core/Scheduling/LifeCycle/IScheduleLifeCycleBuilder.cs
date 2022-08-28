using System;

namespace InEngine.Core.Scheduling.LifeCycle;

public interface IScheduleLifeCycleBuilder
{
    AbstractCommand Command { get; set; }
    IScheduleLifeCycleBuilder Before(Action<AbstractCommand> action);
    IScheduleLifeCycleBuilder After(Action<AbstractCommand> action);
    IScheduleLifeCycleBuilder PingBefore(string url);
    IScheduleLifeCycleBuilder PingAfter(string url);
    IScheduleLifeCycleBuilder WriteOutputTo(string output);
    IScheduleLifeCycleBuilder AppendOutputTo(string output);
    IScheduleLifeCycleBuilder EmailOutputTo(string emailAddress);
}