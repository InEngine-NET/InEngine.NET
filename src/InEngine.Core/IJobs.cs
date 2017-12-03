using InEngine.Core.Scheduling;

namespace InEngine.Core
{
    public interface IJobs : IPluginType
    {
        void Schedule(ISchedule schedule);
    }
}
