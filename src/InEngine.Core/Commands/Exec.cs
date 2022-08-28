using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using InEngine.Core.Exceptions;

namespace InEngine.Core.Commands;

public class Exec : AbstractCommand
{
    [Option('e', "executable", Required = true, HelpText = "The name of the CLI program/command to run.")]
    public string Executable { get; set; }

    [Option('a', "args", HelpText = "Arguments for the CLI program/command.")]
    public string Arguments { get; set; }

    [Option('t', "timeout", DefaultValue = 900,
        HelpText = "The number of seconds to wait before killing the running process.")]
    public int Timeout { get; set; }

    public IDictionary<string, string> ExecWhitelist { get; set; }

    public override async Task Run()
    {
        ExecWhitelist ??= InEngineSettings.Make().ExecWhitelist;
        if (!ExecWhitelist.ContainsKey(Executable))
            throw new CommandFailedException("Executable is not whitelisted.");
        var fileName = ExecWhitelist[Executable];
        if (!File.Exists(fileName))
        {
            var message = $"Cannot run {fileName}. It either does not exist or is inaccessible. Exiting...";
            throw new CommandFailedException(message);
        }
            
        var process = new Process
        {
            StartInfo = new ProcessStartInfo(fileName, Arguments)
            {
                UseShellExecute = false,
                ErrorDialog = false,
                RedirectStandardError = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
            }
        };
        var commandWithArguments = $"{fileName} {Arguments}";
        
        process.Start();
        
        var timeoutSignal = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));
        try 
        {
            await process.WaitForExitAsync(timeoutSignal.Token).ConfigureAwait(false);
        } catch (OperationCanceledException)
        {
            Error($"The command ({commandWithArguments}) has timed out and is about to be killed...");
            process.Kill();
            Error($"The command ({commandWithArguments}) has been killed.");
            var message = $"The command ({commandWithArguments}) timed out after {Timeout} second(s).";
            throw new CommandFailedException(message);
        }
    }
}