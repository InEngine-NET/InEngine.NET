﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using InEngine.Core;
using InEngine.Core.Exceptions;
using InEngine.Core.IO;
using InEngine.Core.Queuing;
using Microsoft.Extensions.Logging;

namespace InEngine;

public class ArgumentInterpreter
{
    public ILogger Log { get; } = LogManager.GetLogger<PluginAssembly>();
    public string CliLogo { get; }
    public IConsoleWrite Write { get; } = new Write();

    public ArgumentInterpreter()
    {
        CliLogo = resources.cliLogo;
    }

    public async Task Interpret(string[] args)
    {
        var pluginAssemblies = PluginAssembly.Load<AbstractPlugin>();
        var parser = new Parser(with => {
            with.IgnoreUnknownArguments = true;
            with.MutuallyExclusive = true;
        });
        var options = new Options();

        if (!parser.ParseArguments(args, options)) 
            return;

        if (!args.Any())
            PrintInEngineHelpTextAndExit(pluginAssemblies, options);

        InEngineSettings.ConfigurationFile = options.ConfigurationFile;

        if (options.ShouldRunServer) 
        {
            Write.Info(CliLogo);
            Write.Line("Starting...").Newline();
            await Program.RunServerAsync();
            ExitWithSuccess();
        }

        var pluginArgs = args.ToArray();
        var firstPluginArg = pluginArgs.FirstOrDefault();
        var firstArgIsConf = firstPluginArg.StartsWith("-c", StringComparison.OrdinalIgnoreCase) ||
                             firstPluginArg.StartsWith("--configuration", StringComparison.OrdinalIgnoreCase);

        if (firstArgIsConf)
            pluginArgs = pluginArgs.Skip(1).ToArray();

        var commandVerbName = pluginArgs.FirstOrDefault();

        foreach(var assembly in pluginAssemblies)
        foreach (var ops in assembly.Plugins)
        foreach (var prop in ops.GetType().GetProperties())
        foreach (var attr in prop.GetCustomAttributes(true))
            if (attr is VerbOptionAttribute commandVerb && (commandVerb.LongName == commandVerbName || commandVerb.ShortName.ToString() == commandVerbName))
                InterpretPluginArguments(pluginArgs, ops);

        PrintInEngineHelpTextAndExit(pluginAssemblies, options);
    }

    public void ExitWithSuccess(string message = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            message = "success";
        Log.LogDebug($"✔ {message}");
        Environment.Exit(ExitCodes.Success);
    }

    public void ExitWithFailure(string message = null)
    {
        Log.LogError(MakeErrorMessage(message));
        Write.Error(message);
        Environment.Exit(ExitCodes.Fail);
    }

    public void ExitWithFailure(Exception exception = null)
    {
        var ex = exception ?? new Exception("Unspecified failure");
        Log.LogError(MakeErrorMessage(ex.Message), ex);
        Write.Error(ex.Message);
        Environment.Exit(ExitCodes.Fail);
    }

    protected string MakeErrorMessage(string message = null)
    {
        if (string.IsNullOrWhiteSpace(message))
            message = "fail";
        return $"✘ {message}";
    }

    public void InterpretPluginArguments(string[] pluginArgs, AbstractPlugin abstractPlugin)
    {
        var isSuccessful = Parser.Default.ParseArguments(pluginArgs, abstractPlugin, (verb, subOptions) => {
            try
            {
                var settings = InEngineSettings.Make();
                if (subOptions == null && (pluginArgs.Contains("-h") || pluginArgs.Contains("--help")))
                    ExitWithSuccess();
                else if (subOptions == null)
                    ExitWithFailure("");
                var command = subOptions as AbstractCommand;
                if (command is AbstractCommand)
                    (command as AbstractCommand).Name = verb.Normalize();
                if (command is IHasQueueSettings)
                    (command as IHasQueueSettings).QueueSettings = settings.Queue;
                command.MailSettings = settings.Mail;
                command.RunAsync(); 
                ExitWithSuccess();
            }
            catch (Exception exception)
            {
                ExitWithFailure(exception);
            }
        });

        if (!isSuccessful)
            ExitWithFailure(new CommandFailedException("Could not parse plugin arguments. Use -h, --help for usage."));
    }

    public void PrintPluginHelpTextAndExit(PluginAssembly plugin, List<AbstractPlugin> pluginOptionList, string[] pluginArgs)
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
            pluginOptions
                .GetType()
                .GetProperties()
                .SelectMany(x => x.GetCustomAttributes(true))
                .Where(x => x is BaseOptionAttribute)
                .ToList()
                .ForEach(x => {
                    var optionAttribute = (x as BaseOptionAttribute);
                    Write.Text($"  {optionAttribute.LongName}".PadRight(20));
                    Write.Line(optionAttribute.HelpText);
                });
        }
        ExitWithSuccess();
    }

    public void PrintInEngineHelpTextAndExit(List<PluginAssembly> pluginAssemblies, Options options)
    {
        Write.Info(CliLogo);
        Write.Text(options.GetUsage(""));

        /*
         * Compute the max width of a command verb name to allow for proper padding.
         */
        var maxWidth = 0;
        pluginAssemblies.ForEach(pluginAssembly => {
            pluginAssembly.Plugins.ForEach(plugin => {
                foreach (var verb in plugin.GetVerbOptions())
                    if (verb.LongName != null && verb.LongName.Length > maxWidth)
                        maxWidth = verb.LongName.Length;
            });
        });

        /*
         * Print out each plugin's commands.
         */
        pluginAssemblies.ForEach(pluginAssembly => {
            Write.Warning(pluginAssembly.Name);
            pluginAssembly
                .Plugins
                .OrderBy(x => x.GetType().Name)
                .ToList()
                .ForEach(plugin => {
                    plugin.GetVerbOptions().ToList().ForEach(verb => {
                        var name = (verb.LongName ?? "");
                        var padding = new string(' ', maxWidth - name.Length + 2);
                        Write.InfoText($"  {name}")
                            .Line(padding + (verb.HelpText ?? ""));
                    });
                });
        });

        ExitWithSuccess();
    }
}