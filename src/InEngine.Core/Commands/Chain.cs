namespace InEngine.Core.Commands;

using System;
using System.Collections.Generic;
using System.Linq;
using InEngine.Core.Exceptions;
using Microsoft.Extensions.Logging;

public class Chain : AbstractCommand
{
    public IList<AbstractCommand> Commands { get; set; } = new List<AbstractCommand>();

    public override void Run()
    {
        Commands.ToList().ForEach(x =>
        {
            try
            {
                x.WriteSummaryToConsole();
                x.Run();
            }
            catch (Exception exception)
            {
                Log.LogError($"Chain command failed at command #{x}", exception);
                x.Failed(exception);
                throw new CommandChainFailedException(x.Name, exception);
            }
        });
    }
}