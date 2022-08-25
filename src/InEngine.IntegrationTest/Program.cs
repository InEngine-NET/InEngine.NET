using System;
using InEngine.Core;

namespace InEngine.IntegrationTest
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine("Queue Integration Tests");
            Console.WriteLine("------------------------------------------------------------");
            new QueuingTest().Run();

            Console.WriteLine("------------------------------------------------------------");
            Console.WriteLine("Start Server...");
            Console.WriteLine("------------------------------------------------------------");
            var settings = InEngineSettings.Make();
            var serverHost = new ServerHost() {
                MailSettings = settings.Mail,
                QueueSettings = settings.Queue,
            };
            serverHost.Start();
            Console.ReadLine();
            serverHost.Dispose();
        }
    }   
}
