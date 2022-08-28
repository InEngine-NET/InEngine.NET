using System;
using System.Collections.Generic;
using System.Linq;
using InEngine.Core.Exceptions;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace InEngine.Core.Commands;

public class Chain : AbstractCommand
{
    public IList<AbstractCommand> Commands { get; set; } = new List<AbstractCommand>();

    public override async Task Run()
    {
        foreach (var x in Commands.ToList())
        {
            try
            {
                x.WriteSummaryToConsole();
                await x.Run();
            }
            catch (Exception exception)
            {
                Log.LogError(exception, "Chain command failed");
                x.Failed(exception);
                throw new CommandChainFailedException(x.Name, exception);
            }
        }
    }
}