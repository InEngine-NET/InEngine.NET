using System;
using System.Threading.Tasks;
using InEngine.Core.Exceptions;

namespace InEngine.Core.Commands;

/// <summary>
/// Dummy command for testing.
/// </summary>
public class AlwaysFail : AbstractCommand
{
    public override async Task RunAsync()
    {
        throw new CommandFailedException("This command always fails.");
    }

    public override void Failed(Exception exception)
    {
        Error(exception.Message);
        if (exception.InnerException != null)
            Error(exception.InnerException.Message);
    }
}