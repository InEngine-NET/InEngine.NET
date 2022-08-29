using System;
using System.IO;
using System.Threading.Tasks;
using InEngine.Core;

namespace InEngine;

public static class Program
{
    public static ServerHost ServerHost { get; set; }

    private static async Task Main(string[] args)
    {
        /*
         * Set current working directory as services use the system directory by default.
         * Also, allow running the CLI from a different directory than the application root.
         */
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
        await new ArgumentInterpreter().Interpret(args);
    }

    public static async Task RunServerAsync()
    {
        var settings = InEngineSettings.Make();
        ServerHost = new ServerHost
        {
            MailSettings = settings.Mail,
            QueueSettings = settings.Queue,
        };

        await ServerHost.StartAsync();
        Console.WriteLine(resources.foregroundServerInputPrompt);
        Console.ReadLine();
        ServerHost.Dispose();
    }
}