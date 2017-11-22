using System;
using System.IO;
using System.ServiceProcess;

namespace InEngine
{
    class Program
    {
        public const string ServiceName = "InEngine.NET";
        public static ServerHost ServerHost { get; set; }

        static void Main(string[] args)
        {
            new ArgumentInterpreter().Interpret(args);
        }

        /// <summary>
        /// Start the scheduler as a service or as a CLI program in the foreground.
        /// </summary>
        public static void RunScheduler()
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
                var serverHost = new ServerHost();
                serverHost.Start();
                Console.WriteLine("Press any key to stop...");
                Console.ReadLine();
                serverHost.Dispose();
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
