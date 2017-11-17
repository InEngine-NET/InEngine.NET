using System;
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
            var plugins = Plugin.Discover<IOptions>();
            if (!args.Any())
            {
                Console.WriteLine("Available plugins... ");
                plugins.ForEach(x => Console.WriteLine(x.Name));
                ExitWithSuccess();
            }

            var parser = new Parser(with => with.IgnoreUnknownArguments = true);
            var options = new Options();

            if (parser.ParseArguments(args, options))
            {
                if (options == null)
                    Environment.Exit(Parser.DefaultExitCodeFail);

                var plugin = plugins.FirstOrDefault(x => x.Name == options.PluginName);
                if (plugin == null)
                    ExitWithFailure("Plugin does not exist: " + options.PluginName);
                
                var pluginOptionList = plugin.Make<IOptions>();

                var pluginArgs = args.Skip(1).ToArray();
                if (!pluginArgs.ToList().Any()) {
                    // If the plugin's args are empty, print the plugin's help screen and exit.
                    foreach(var pluginOptions in pluginOptionList) {
                        Parser.Default.ParseArguments(pluginArgs, pluginOptions);
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
            Logger.Error(MakeErrorMessage(message));
            Environment.Exit(Parser.DefaultExitCodeFail);
        }

        public void ExitWithFailure(Exception exception = null)
        {
            var ex = exception ?? new Exception("Unspecified failure");
            Logger.Error(ex, MakeErrorMessage(ex.Message));
            Environment.Exit(Parser.DefaultExitCodeFail);
        }

        protected string MakeErrorMessage(string message = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                message = "fail";
            return $"✘ {message}";
        }

        public void InterpretPluginArguments(string[] pluginArgs, IOptions pluginOptions)
        {
            var isSuccessful = Parser
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
