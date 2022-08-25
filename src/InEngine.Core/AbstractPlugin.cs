using System.Collections.Generic;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using InEngine.Core.Scheduling;

namespace InEngine.Core
{
    abstract public class AbstractPlugin : IPlugin
    {
        public virtual void Schedule(ISchedule schedule)
        {}

        [HelpVerbOption]
        public virtual string GetUsage(string verb)
        {
            var helpText = HelpText.AutoBuild(this, verb);
            helpText.Heading = $"{GetType().Assembly.GetName().Name} {GetType().Assembly.GetName().Version.ToString()}";
            return helpText;
        }

        public IList<VerbOptionAttribute> GetVerbOptions()
        {
            return GetType()
                .GetProperties()
                .SelectMany(property => {
                    return property.GetCustomAttributes(typeof(VerbOptionAttribute), true)
                                   .Select(verb => verb as VerbOptionAttribute);
                })
                .OrderBy(x => x.LongName)
                .ToList();
        }
    }
}
