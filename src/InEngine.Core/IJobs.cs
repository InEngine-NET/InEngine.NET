using Quartz;

namespace InEngine.Core
{
    public interface IJobs : IPluginType
    {
        void Schedule(IScheduler scheduler);
    }
}
