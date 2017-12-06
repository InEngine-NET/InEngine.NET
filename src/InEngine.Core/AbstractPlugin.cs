using System;
using CommandLine;
using CommandLine.Text;
using InEngine.Core.Scheduling;

namespace InEngine.Core
{
    public class AbstractPlugin : IPluginType
    {
        public virtual void Schedule(ISchedule schedule)
        {}

        [HelpVerbOption]
        public virtual string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
