using System;
using System.Linq;
using InEngine.Core.Queuing.Commands;
using InEngine.Core.Scheduling;

namespace InEngine.Core.Queuing
{
    public class CommandSchedule : AbstractCommand
    {
        public void Schedule(ISchedule schedule)
        {
            var queueSettings = InEngineSettings.Make().Queue;
            ScheduleQueueConsumerJobs(schedule, queueSettings.PrimaryQueueConsumers);
            ScheduleQueueConsumerJobs(schedule, queueSettings.SecondaryQueueConsumers, true);
        }

        void ScheduleQueueConsumerJobs(ISchedule schedule, int consumers, bool useSecondaryQueue = false)
        {
            if (consumers < 0)
                throw new ArgumentOutOfRangeException(nameof(consumers), consumers, "The number of queue consumers must be 0 or greater.");

            foreach (var index in Enumerable.Range(0, consumers).ToList())
                schedule.Command(new Consume()
                {
                    ScheduleId = $"{(useSecondaryQueue ? "secondary" : "primary")}:{index.ToString()}",
                    UseSecondaryQueue = useSecondaryQueue
                })
                .EverySecond();
        }
    }
}
