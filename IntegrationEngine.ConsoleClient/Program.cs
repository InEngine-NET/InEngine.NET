using IntegrationEngine.Client;
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
            var client = new InEngineClient();
            var triggers = client.GetCronTriggers();
            Console.WriteLine(triggers.Count);
            Console.ReadLine();
        }
    }
}
