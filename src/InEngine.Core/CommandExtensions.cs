using System;

namespace InEngine.Core
{
    public static class CommandExtensions
    {
        public static void WriteSummaryToConsole(this AbstractCommand command) =>
            Console.WriteLine($"⚡ {command.Name} : {command.ScheduleId} | Try #{command.CommandLifeCycle.CurrentTry}");
    }
}