using System.Diagnostics;
using System.IO;
using CommandLine;
using InEngine.Core.Exceptions;

namespace InEngine.Core.Commands
{
    public class SystemProcess : AbstractCommand
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
            process.Start();
            if (process.WaitForExit(Timeout * 1000)) {
                return;
            }
            Error($"The command ({commandWithArguments}) has timed out and is about to be killed...");
            process.Kill();
            Error($"The command ({commandWithArguments}) has been killed.");
            throw new CommandFailedException($"The command ({commandWithArguments}) timed out after {Timeout} second(s).");
        }
    }
}
