namespace InEngine.Core.Commands;

/// <summary>
/// Dummy command for testing.
/// </summary>
public class AlwaysSucceed : AbstractCommand
{
    public override void Run() => Info("This command always succeeds.");
}