using CommandLine;
using IntegrationEngine.Model;
using System;
using IntegrationEngine.Client;

namespace IntegrationEngine.ConsoleClient
{
    public class GetSubOptions
    {
        [Option('e', "endpoint", Required = true, HelpText="Endpoint to consume.")]
        public Endpoint Resource { get; set; }

        [Option('i', "id", HelpText="Id of endpoint to consume.")]
        public string Id { get; set; }
    }
}
