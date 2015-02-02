using CommandLine;
using CommandLine.Text;
using IntegrationEngine.Client;
using IntegrationEngine.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntegrationEngine.ConsoleClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (args.Length <= 1) {
                Console.WriteLine(options.GetUsage());
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }
            var invokedVerb = "";
            var invokedVerbInstance = new object();
            if (!CommandLine.Parser.Default.ParseArguments(args, options, (verb, subOptions) => {
                invokedVerb = verb;
                invokedVerbInstance = subOptions;
            }))
            {
                Console.WriteLine(options.GetUsage());
                Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
            }

            var client = !string.IsNullOrWhiteSpace(options.WebApiUrl) ? 
                new InEngineClient(options.WebApiUrl) :
                new InEngineClient();

            if (invokedVerb == "get") {
                var getSubOptions = (GetSubOptions)invokedVerbInstance;
                switch (getSubOptions.Resource)
                {
                case Endpoint.CronTrigger:
                    ResolveResult(client.GetCollection<CronTrigger>());
                    break;
                case Endpoint.SimpleTrigger:
                    ResolveResult(client.GetCollection<SimpleTrigger>());
                    break;
                case Endpoint.JobType:
                    ResolveResult(client.GetCollection<JobType>());
                    break;
                case Endpoint.TimeZone:
                    ResolveResult(client.GetCollection<IntegrationEngine.Model.TimeZone>());
                    break;
                case Endpoint.HealthStatus:
                    ResolveResult(client.GetHealthStatus());
                    break;
                }
            }
        }

        public static void ResolveResult(dynamic result)
        {
            if (result == null)
                Console.WriteLine("The web API did not return a result. Is the URL correct and the server online? Try -mPing.");
            else if (result is IEnumerable) 
            {
                Console.WriteLine("Number of items: {0}", result.Count);
                foreach (var item in result)
                    Console.WriteLine(item);
            }
            else
                Console.WriteLine(result);
        }
    }
}
