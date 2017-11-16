using System;
using System.Linq;
using System.Reflection;
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
            var parser = new CommandLine.Parser(with => with.IgnoreUnknownArguments = true);
            var options = new Options();

            if (parser.ParseArguments(args, options))
            {
                if (options == null)
                    Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);

                // Get possible types from plugin assembly.
                var targetAssembly = Assembly.LoadFrom(options.PlugInName + ".dll");
                var types = targetAssembly.GetTypes();
                var optionType = types.FirstOrDefault(x => x.IsClass && typeof(IOptions).IsAssignableFrom(x));
                if (optionType == null)
                    Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);

                // Create an instance of the plugin's options class.
                var pluginOptions = targetAssembly.CreateInstance(optionType.FullName) as IOptions;

                // If the plugin's args are empty, print the plugin's help screen and exit.
                var pluginArgs = args.Skip(1).ToArray();
                if (!pluginArgs.ToList().Any()) {
                    CommandLine.Parser.Default.ParseArguments(pluginArgs, pluginOptions);
                    Console.WriteLine(pluginOptions.GetUsage(""));
                    ExitWithSuccess();
                }

                InterpretPluginArguments(pluginArgs, pluginOptions);
            }
        }

        public void ExitWithSuccess(string message = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                message = "success";
            Logger.Debug($"✔ {message}");
            Environment.Exit(0);
        }

        public void ExitWithFailure(Exception exception = null)
        {
            Logger.Error(exception ?? new CommandFailedException(), "✘ fail");
            Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
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
                ExitWithFailure();
        }
    }
}
