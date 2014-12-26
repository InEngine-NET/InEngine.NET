using System;
using TryQuartz.Jobs;

namespace TryQuartz
{
    public class RJob : AsyncJob
    {
        public RJob()
        {}

        public override void Execute(Quartz.IJobExecutionContext context)
        {
            // Need to indicate which job to run
            MessageQueueClient.Publish("run job");
        }
    }
}
