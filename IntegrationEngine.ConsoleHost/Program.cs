using System;
using System.IO;
using System.Reflection;
using System.ServiceProcess;

namespace IntegrationEngine.ConsoleHost
{
    public class Program
    {
        public const string ServiceName = "InEngine.NET Server";
        public static EngineHost EngineHosts { get; set; }
        public static void Main(string[] args)
        {
            Start(args);
            Console.WriteLine("Press any key to stop...");
            Console.ReadLine();
            Stop();
            if (!Environment.UserInteractive)
            {
                // Set current working directory as services use the system directory by default.
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
                using (var service = new Service())
                    ServiceBase.Run(service);
            }
            else
            {
                Start(args);
                Console.WriteLine("Press any key to stop...");
                Console.ReadLine();
                Stop();
            }
        }

        private static void Start(string[] args)
        {
            EngineHosts = new EngineHost(typeof(Program).Assembly);
            EngineHosts.Initialize();
        }

        private static void Stop()
        {
            EngineHosts.Dispose();
        }

        public class Service : ServiceBase
        {
            public Service()
            {
                ServiceName = Program.ServiceName;
            }

            protected override void OnStart(string[] args)
            {
                Program.Start(args);
            }

            protected override void OnStop()
            {
                Program.Stop();
            }
        }
    }
}
