using System;
using InEngine.Core;
using InEngine.Core.Exceptions;

namespace InEngine.Commands;

/// <summary>
/// Dummy command for testing and sample code.
/// </summary>
public class AlwaysFail : AbstractCommand
{
    public override void Run()
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