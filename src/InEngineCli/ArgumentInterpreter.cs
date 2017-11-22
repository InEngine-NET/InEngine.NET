using System;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using InEngine.Core;
using InEngine.Core.Exceptions;
using NLog;
using InEngine.Core.IO;

namespace InEngineCli
{
    public class ArgumentInterpreter
    {
        public Logger Logger { get; set; }
        public string CliLogo { get; set; }
        public ArgumentInterpreter()
        {
            Logger = LogManager.GetCurrentClassLogger();
            CliLogo = @"
  ___       _____             _              _   _ _____ _____ 
 |_ _|_ __ | ____|_ __   __ _(_)_ __   ___  | \ | | ____|_   _|
  | || '_ \|  _| | '_ \ / _` | | '_ \ / _ \ |  \| |  _|   | |  
  | || | | | |___| | | | (_| | | | | |  __/_| |\  | |___  | |  
 |___|_| |_|_____|_| |_|\__, |_|_| |_|\___(_|_| \_|_____| |_|  
                        |___/ 
";
        }

        public void Interpret(string[] args)
        {
            var write = new Write();
            var plugins = Plugin.Discover<IOptions>();
            if (!args.Any())
            {
                write.Info(CliLogo);
                write.Warning("Usage:");
                write.Line($"  -p[<plugin_name>] [<command_name>]");
                write.Newline();
                write.Warning("Plugins:");
                plugins.ForEach(x => Console.WriteLine($"  {x.Name}"));
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
                    write.Info(CliLogo);

                    write.WarningText("Plugin: ");
                    write.LineText($"{plugin.Name}");
                    write.Newline().Newline();
                    write.Warning("Commands:");
                    // If the plugin's args are empty, print the plugin's help screen and exit.
                    foreach(var pluginOptions in pluginOptionList) {
                        Parser.Default.ParseArguments(pluginArgs, pluginOptions);

                        var verbs = pluginOptions
                            .GetType()
                            .GetProperties()
                            .SelectMany(x => x.GetCustomAttributes(true))
                            .Where(x => x is BaseOptionAttribute)
                            .ToList();

                        verbs.ForEach(x => {
                            var optionAttribute = (x as BaseOptionAttribute);
                            Console.WriteLine($"  {optionAttribute.LongName}\t{optionAttribute.HelpText}");
                        });

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
            new Write().Error(message);
            Environment.Exit(Parser.DefaultExitCodeFail);
        }

        public void ExitWithFailure(Exception exception = null)
        {
            var ex = exception ?? new Exception("Unspecified failure");
            Logger.Error(ex, MakeErrorMessage(ex.Message));
            new Write().Error(ex.Message);
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
            var isSuccessful =Parser.Default.ParseArguments(pluginArgs, pluginOptions, (verb, subOptions) => {
                try
                {
                    var lastArg = pluginArgs.ToList().LastOrDefault();
                    if (subOptions == null && (lastArg == "-h" || lastArg == "--help"))
                        ExitWithSuccess();
                    else if (subOptions == null)
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
