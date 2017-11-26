using System;
using System.Diagnostics;
using System.IO;
using CommandLine;
using InEngine.Core.Exceptions;

namespace InEngine.Core.Commands
{
    public class RuntimeProcess : AbstractCommand
    {
        [Option('c', "command", Required = true, HelpText = "The name of the CLI program/command to run.")]
        public string Command { get; set; }

        [Option('a', "args", HelpText = "Arguments for the CLI program/command.")]
        public string Arguments { get; set; }

        [Option('t', "timeout", DefaultValue = 900, HelpText = "The number of seconds to wait before killing the runnin process.")]
        public int Timeout { get; set; }

        public override void Run()
        {
            if (!File.Exists(Command))
                throw new CommandFailedException($"Cannot run {Command}. It either does not exist or is inaccessible. Exiting...");
            var process = new Process() { 
                StartInfo = new ProcessStartInfo(Command, Arguments) {
                    UseShellExecute = false,
                    ErrorDialog = false,
                    RedirectStandardError = false,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = false,
                } 
            };
            var commandWithArguments = $"{Command} {Arguments}";
            Logger.Debug($"Starting process for command: {commandWithArguments}");
            process.Start();
            Logger.Debug($"Command ({commandWithArguments}) started with process ID {process.Id}.");
            if (process.WaitForExit(Timeout * 1000)) {
                Logger.Debug($"Process for command {commandWithArguments} with PID {process.Id} ended with exit code {process.ExitCode}.");
                return;
            }
            Logger.Debug($"The command ({commandWithArguments}) has timed out and is about to be killed...");
            process.Kill();
            Logger.Debug($"The command ({commandWithArguments}) has been killed.");
            throw new CommandFailedException($"The command timed out after {Timeout} second(s).");
        }
    }
}
