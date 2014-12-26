using System;
using Quartz;

namespace TryQuartz.Jobs
{
    public class HelloJob : IJob
    {
        public HelloJob()
        {
        }

        public void Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Greetings from HelloJob!");
        }
    }
}

