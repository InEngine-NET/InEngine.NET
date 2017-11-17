using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using InEngine.Core;
using InEngine.Core.Exceptions;
using NLog;

namespace InEngineCli
{
    public class ArgumentInterpreter
    {
        public Logger Logger { get; set; }

        public ArgumentInterpreter()
        {
            Logger = LogManager.GetCurrentClassLogger();
        }

        public void Interpret(string[] args)
        {
            var plugins = FindPlugins();
            if (!args.Any())
            {
                Console.WriteLine("Available plugins... ");
                plugins.ForEach(x => Console.WriteLine(x.Name));
                ExitWithSuccess();
            }

            var parser = new CommandLine.Parser(with => with.IgnoreUnknownArguments = true);
            var options = new Options();

            if (parser.ParseArguments(args, options))
            {
                if (options == null)
                    Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);

                var plugin = plugins.FirstOrDefault(x => x.Name == options.PlugInName);
                if (plugin == null)
                    ExitWithFailure("Plugin does not exist: " + options.PlugInName);
                
                var pluginOptionList = plugin.MakeOptions();

                var pluginArgs = args.Skip(1).ToArray();
                if (!pluginArgs.ToList().Any()) {
                    // If the plugin's args are empty, print the plugin's help screen and exit.
                    foreach(var pluginOptions in pluginOptionList) {
                        CommandLine.Parser.Default.ParseArguments(pluginArgs, pluginOptions);
                        Console.WriteLine(pluginOptions.GetUsage(""));
                    }
                    ExitWithSuccess();
                }

                var commandVerbName = pluginArgs.First();
                foreach (var ops in pluginOptionList)
                    foreach (var prop in ops.GetType().GetProperties())
                        foreach (object attr in prop.GetCustomAttributes(true))
                            if (attr is VerbOptionAttribute commandVerb && (commandVerb.LongName == commandVerbName || commandVerb.ShortName.ToString() == commandVerbName))
                                    InterpretPluginArguments(pluginArgs, ops);
            }
        }

        public void ExitWithSuccess(string message = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                message = "success";
            Logger.Debug($"✔ {message}");
            Environment.Exit(0);
        }

        public void ExitWithFailure(string message = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                message = "fail";
            Logger.Error($"✘ {message}");
            Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
        }

        public void ExitWithFailure(Exception exception = null)
        {
            Logger.Error(exception ?? new CommandFailedException(), "✘ fail");
            Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
        }

        public List<Plugin> FindPlugins()
        {
            return Directory
                .GetFiles(".", "*.dll")
                .Select(x => Assembly.LoadFrom(x))
                .Where(x => x.GetTypes().Any(y => y.IsClass && typeof(IOptions).IsAssignableFrom(y)))
                .Select(x => new Plugin(x))
                .ToList();
        }

        public void InterpretPluginArguments(string[] pluginArgs, IOptions pluginOptions)
        {
            var isSuccessful = CommandLine
                .Parser
                .Default
                .ParseArguments(pluginArgs, pluginOptions, (verb, subOptions) =>
                {
                    try
                    {
                        if (subOptions == null)
                            ExitWithFailure(new CommandFailedException("Could not parse plugin options"));

                        var command = subOptions as ICommand;

                        if (command is AbstractCommand)
                            (command as AbstractCommand).Name = verb.Normalize();

                        var commandResult = command.Run();

                        if (commandResult.IsSuccessful)
                            ExitWithSuccess(commandResult.Message);
                        else
                            ExitWithFailure(new CommandFailedException(commandResult.Message));
                    }
                    catch (Exception exception)
                    {
                        ExitWithFailure(exception);
                    }
                });

            if (!isSuccessful)
                ExitWithFailure("Could not parse plugin arguments");
        }
    }
}
