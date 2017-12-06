using InEngine.Core.Scheduling;

namespace InEngine.Core
{
    public interface IPluginType
    {
        void Schedule(ISchedule schedule);
        string GetUsage(string verb);
    }
}
