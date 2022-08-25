using InEngine.Core.Scheduling;

namespace InEngine.Core
{
    public interface IPlugin
    {
        void Schedule(ISchedule schedule);
        string GetUsage(string verb);
    }
}
