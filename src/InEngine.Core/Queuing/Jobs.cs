using System;
using System.Linq;
using InEngine.Core.Queuing.Commands;
using InEngine.Core.Scheduling;

namespace InEngine.Core.Queuing
{
    public class Jobs : IJobs
    {
        public void Schedule(Schedule schedule)
        {
            var queueSettings = InEngineSettings.Make().Queue;
            ScheduleQueueConsumerJobs(schedule, queueSettings.PrimaryQueueConsumers);
            ScheduleQueueConsumerJobs(schedule, queueSettings.SecondaryQueueConsumers, true);
        }

        private void ScheduleQueueConsumerJobs(Schedule schedule, int consumers, bool useSecondaryQueue = false)
        {
            if (consumers < 0)
                throw new ArgumentOutOfRangeException(nameof(consumers), consumers, "The number of queue consumers must be 0 or greater.");

            foreach (var index in Enumerable.Range(0, consumers).ToList())
                schedule.Job(new Consume()
                {
                    ScheduleId = $"{(useSecondaryQueue ? "secondary" : "primary")}:{index.ToString()}",
                    UseSecondaryQueue = useSecondaryQueue
                })
                        .EverySecond();
                //.Before(x => Console.Write("Before..."))
                //.After(x => Console.Write("After..."));
        }
    }
}
