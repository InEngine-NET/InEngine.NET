using CommandLine;
using System;
using System.Collections.Generic;
using IntegrationEngine.Client;

namespace IntegrationEngine.ConsoleClient
{

    public class Options
    {
        [Option('m', "method", Required = true, HelpText = "Call a method from IntegrationEngine.Client.")]
        public string MethodName { get; set; }

        [Option('u', "url", Required = false, HelpText = "Web API url.")]
        public string WebApiUrl { get; set; }
    }
}
