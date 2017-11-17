using System;
using System.IO;
using System.ServiceProcess;

namespace InEngineScheduler
{
    class Program
    {
        public const string ServiceName = "InEngine Server";
        public static ServerHost ServerHost { get; set; }
        public static void Main(string[] args)
        {
            var isRunningUnderMono = Type.GetType("Mono.Runtime") != null;
            if (!Environment.UserInteractive && !isRunningUnderMono)
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

        static void Start(string[] args)
        {
            ServerHost = new ServerHost();
            ServerHost.Start();
        }

        static void Stop()
        {
            ServerHost.Dispose();
        }

        public class Service : ServiceBase
        {
            public Service()
            {
                ServiceName = Program.ServiceName;
            }

            protected override void OnStart(string[] args)
            {
                Start(args);
            }

            protected override void OnStop()
            {
                Stop();
            }
        }
    }
}
