using System;
using CommandLine;
using CommandLine.Text;

namespace InEngine.Core
{
    public class AbstractPlugin : IPluginType
    {
        [HelpVerbOption]
        public virtual string GetUsage(string verb)
        {
            return HelpText.AutoBuild(this, verb);
        }
    }
}
