namespace InEngine.Core.Commands
{
    /// <summary>
    /// Dummy command for testing and sample code.
    /// </summary>
    public class Null : AbstractCommand
    {
        public override CommandResult Run()
        {
            return new CommandResult(true);
        }
    }
}
