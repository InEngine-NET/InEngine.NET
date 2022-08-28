using System;
using System.Collections.Generic;
using System.Linq;
using InEngine.Core.Exceptions;
using Microsoft.Extensions.Logging;

namespace InEngine.Core.Commands;

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
                Log.LogError(exception, "Chain command failed");
                x.Failed(exception);
                throw new CommandChainFailedException(x.Name, exception);
            }
        });
    }
}