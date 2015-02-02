using CommandLine;
using System;
using System.Collections.Generic;
using IntegrationEngine.Client;
using System.Text;
using System.Reflection;

namespace IntegrationEngine.ConsoleClient
{
    public class Options
    {
        [Option('u', "url", Required = false, HelpText = "Web API url.")]
        public string WebApiUrl { get; set; }

        [VerbOption("get", HelpText = "Consume a resource.")]
        public GetSubOptions GetVerb { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            var version = (Attribute.GetCustomAttribute(
                Assembly.GetEntryAssembly(), 
                typeof(AssemblyInformationalVersionAttribute)) 
                as AssemblyInformationalVersionAttribute).InformationalVersion;
            var usage = new StringBuilder();
            usage.AppendLine(string.Format("InEngine.NET Console Client {0}", version));
            usage.AppendLine("Project website: http://inengine.net");
            return usage.ToString();
        }
    }
}
