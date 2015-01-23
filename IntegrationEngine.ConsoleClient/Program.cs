using CommandLine;
using CommandLine.Text;
using IntegrationEngine.Client;
using IntegrationEngine.Model;
using Newtonsoft.Json;
using System;
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
            if (!CommandLine.Parser.Default.ParseArguments(args, options))
                return;
            var client = !string.IsNullOrWhiteSpace(options.WebApiUrl) ? 
                new InEngineClient(options.WebApiUrl) :
                new InEngineClient();
            var type = client.GetType();
            if (!type.GetMethods().Where(x => x.Name == options.MethodName && !x.GetParameters().Any()).Any())
            {
                Console.WriteLine("Method '{0}' does not exist", options.MethodName);
                var availableMethods = typeof(IInEngineClient).GetMethods()
                    .Where(x => !x.GetParameters().Any())
                    .Select(x => x.Name)
                    .ToList();
                Console.WriteLine("Available methods: '{0}'", string.Join(", ", availableMethods));
                return;
            }
            var method = type.GetMethod(options.MethodName);
            dynamic result = method.Invoke(client, null);
            if (result == null)
                Console.WriteLine("The web API did not return a result. Is the URL correct and the server online? Try -mPing.");
            else if (method.ReturnType.GetInterface("IEnumerable") != null) 
                foreach (var item in result)
                    Console.WriteLine(item);
            else
                Console.WriteLine(result);
        }
    }
}
