﻿using System;
using System.IO;
using InEngine.Core;

namespace InEngine
{
    class Program
    {
        public const string ServiceName = "InEngine.NET";
        public static ServerHost ServerHost { get; set; }

        private static void Main(string[] args)
        {
            /*
             * Set current working directory as services use the system directory by default.
             * Also, maybe run from the CLI from a different directory than the application root.
             */
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            new ArgumentInterpreter().Interpret(args);
        }

        public static void RunServer()
        {
            var settings = InEngineSettings.Make();
            ServerHost = new ServerHost
            {
                MailSettings = settings.Mail,
                QueueSettings = settings.Queue,
            };

            ServerHost.Start();
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
            ServerHost.Dispose();
        }
    }
}