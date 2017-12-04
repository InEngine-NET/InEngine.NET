using InEngine.Core.Scheduling;

namespace InEngine.Core
{
    public interface ICommandSchedule : IPluginType
    {
        void Schedule(ISchedule schedule);
    }
}
