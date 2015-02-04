using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using IntegrationEngine.Client;
using System.Text;
using System.Reflection;

namespace IntegrationEngine.ConsoleClient
{
    public class Options
    {
        [Option('u', "url", Required = false, HelpText = "Web API url.")]
        public string WebApiUrl { get; set; }

        [VerbOption("get", HelpText = "Consume an endpoint.")]
        public GetSubOptions GetVerb { get; set; }

        [VerbOption("ping", HelpText = "Ping the InEngine server Web API.")]
        public PingSubOptions PingVerb { get; set; }

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
            var verbAttributes = 
                this.GetType()
                    .GetProperties()
                    .Where(prop => prop.IsDefined(typeof(VerbOptionAttribute), false));
            foreach (var verbAttribute in verbAttributes) {
                var attributeInstances = (VerbOptionAttribute[])verbAttribute.GetCustomAttributes(typeof(VerbOptionAttribute), false);
                foreach (var instance in attributeInstances)
                    usage.AppendLine(string.Format("{0} - {1}", instance.LongName, instance.HelpText));
            }
            return usage.ToString();
        }
    }
}
