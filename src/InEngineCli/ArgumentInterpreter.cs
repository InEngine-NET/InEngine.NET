using System;
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
            var options = new Options();
            var isSuccessful = CommandLine.Parser.Default.ParseArguments(args, options, (verb, subOptions) =>
            {
                try
                {
                    if (subOptions == null)
                        Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);

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

        public void ExitWithSuccess(string message)
        {
            Logger.Debug("Command successful: " + message);
            Environment.Exit(0);
        }

        public void ExitWithFailure(Exception exception = null)
        {
            if (exception != null)
                Logger.Error(exception, "Command failed");
            Environment.Exit(CommandLine.Parser.DefaultExitCodeFail);
        }
    }
}
