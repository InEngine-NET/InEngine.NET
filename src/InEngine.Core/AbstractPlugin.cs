using System;
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
        public string GetUsage(string verb)
        {
            var helpText = HelpText.AutoBuild(this, verb);
            helpText.Heading = $"{GetType().Assembly.GetName().Name} v{GetType().Assembly.GetName().Version.ToString()}";
            return helpText;
        }

        public virtual string GetUsageWithoutHeader()
        {
            var verbs = GetType()
                .GetProperties()
                .ToList()
                .SelectMany(x => {
                    return x.GetCustomAttributes(typeof(VerbOptionAttribute), true).Select(y => y as VerbOptionAttribute);
                });
            var maxWidth = 0;
            foreach (var verb in verbs)
                if (verb.LongName != null && verb.LongName.Length > maxWidth)
                    maxWidth = verb.LongName.Length;

            var helpTextLine = verbs.Select(x => {
                var name = (x.LongName ?? "");
                var padding = maxWidth - name.Length + 2;
                return $"  {name}" + string.Join("", Enumerable.Range(0, padding).Select(y => " ")) + (x.HelpText ?? "");
            });

            return string.Join(Environment.NewLine, helpTextLine);
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
