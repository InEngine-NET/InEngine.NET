using System;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using InEngine.Core;
using InEngine.Core.Exceptions;
using NLog;
using InEngine.Core.IO;
using System.ServiceProcess;
using System.IO;
using System.Collections.Generic;

namespace InEngine
{
    public class ArgumentInterpreter
    {
        public Logger Logger { get; set; }
        public string CliLogo { get; set; }
        public IWrite Write { get; set; } = new Write();

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
            var plugins = Plugin.Discover<IOptions>();
            var parser = new Parser(with => {
                with.IgnoreUnknownArguments = true;
                with.MutuallyExclusive = true;
            });
            var options = new Options();

            if (parser.ParseArguments(args, options))
            {
                if (options == null)
                    Environment.Exit(Parser.DefaultExitCodeFail);

                if (!args.Any())
                    PrintInEngineHelpTextAndExit(plugins, options);

                InEngineSettings.ConfigurationFile = options.ConfigurationFile;

                if (options.ShouldRunScheduler) 
                {
                    Program.RunScheduler();
                    ExitWithSuccess();
                }

                var plugin = plugins.FirstOrDefault(x => x.Name == options.PluginName);
                if (plugin == null)
                    ExitWithFailure("Plugin does not exist: " + options.PluginName);
                
                var pluginOptionList = plugin.Make<IOptions>();

                var pluginArgs = args.Skip(1).ToArray();

                if (!pluginArgs.ToList().Any()) {
                    PrintPluginHelpTextAndExit(plugin, pluginOptionList, pluginArgs);
                }

                if (new[] { "-p", "--plugin-name", "-c", "--configuration" }.Any(c => pluginArgs.First().StartsWith("-p", StringComparison.OrdinalIgnoreCase)))
                    pluginArgs = pluginArgs.Skip(1).ToArray();

                // Need to remove plugin options    
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
            Write.Error(message);
            Environment.Exit(Parser.DefaultExitCodeFail);
        }

        public void ExitWithFailure(Exception exception = null)
        {
            var ex = exception ?? new Exception("Unspecified failure");
            Logger.Error(ex, MakeErrorMessage(ex.Message));
            Write.Error(ex.Message);
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

                    command.Run();
                    ExitWithSuccess();
                }
                catch (Exception exception)
                {
                    ExitWithFailure(exception);
                }
            });

            if (!isSuccessful)
                ExitWithFailure("Could not parse plugin arguments");
        }

        public void PrintPluginHelpTextAndExit(Plugin plugin, List<IOptions> pluginOptionList, string[] pluginArgs)
        {
            Write.Info(CliLogo);
            Write.Warning("Plugin: ");
            Write.Line($"  Name:    {plugin.Name}");
            Write.Line($"  Version: {plugin.Version}");
            Write.Newline().Newline();
            Write.Warning("Commands:");
            // If the plugin's args are empty, print the plugin's help screen and exit.
            foreach (var pluginOptions in pluginOptionList)
            {
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

        public void PrintInEngineHelpTextAndExit(List<Plugin> plugins, Options options)
        {
            Write.Info(CliLogo);
            Write.Warning("Usage:");
            Write.LineText(options.GetUsage(""));
            Write.Newline();
            Write.Warning("Plugins:");
            plugins.ForEach(x => Console.WriteLine($"  {x.Name}"));
            ExitWithSuccess();   
        }
    }
}
