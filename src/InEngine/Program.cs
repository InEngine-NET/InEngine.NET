using System;
using System.IO;
using System.ServiceProcess;
using InEngine.Core;
//using Mono.Unix;
//using Mono.Unix.Native;

namespace InEngine
{
    class Program
    {
        public const string ServiceName = "InEngine.NET";
        public static ServerHost ServerHost { get; set; }

        static void Main(string[] args)
        {
            /*
             * Set current working directory as services use the system directory by default.
             * Also, maybe run from the CLI from a different directory than the application root.
             */
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            new ArgumentInterpreter().Interpret(args);
        }

        /// <summary>
        /// Start the server as a service or as a CLI program in the foreground.
        /// </summary>
        public static void RunServer()
        {
            var settings = InEngineSettings.Make();
            ServerHost = new ServerHost() {
                MailSettings = settings.Mail,
                QueueSettings = settings.Queue,
            };

            if (!Environment.UserInteractive && Type.GetType("Mono.Runtime") == null)
            {
                using (var service = new Service())
                    ServiceBase.Run(service);
            }
            else
            {
                ServerHost.Start();
                Console.WriteLine("Press any key to exit...");
                Console.ReadLine();
                ServerHost.Dispose();
            }
        }

        static void Start(string[] args)
        {
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
