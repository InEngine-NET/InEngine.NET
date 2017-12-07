using System;
using CommandLine;
using CommandLine.Text;
using InEngine.Core.Scheduling;

namespace InEngine.Core
{
    public class AbstractPlugin : IPlugin
    {
        public virtual void Schedule(ISchedule schedule)
        {}

        [HelpVerbOption]
        public string GetUsage(string verb)
        {
            var helpText = HelpText.AutoBuild(this, verb);
            helpText.Heading = $"{GetType().Assembly.GetName().Name} v{GetType().Assembly.GetName().Version.ToString()}";
            return helpText;
        }

        public virtual string GetUsageWithoutHeader()
        {
            var helpText = new HelpText();
            helpText.AddOptions(this);
            return helpText;
        }
    }
}
