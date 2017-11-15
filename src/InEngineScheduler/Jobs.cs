using InEngine.Commands.Sample;
using QueueJobs = InEngine.Core.Queue.Jobs;
using Quartz;

namespace InEngineScheduler
{
    public static class Jobs
    {
        public static void Schedule(IScheduler scheduler)
        {
            QueueJobs.Schedule(scheduler);
        }
    }
}
