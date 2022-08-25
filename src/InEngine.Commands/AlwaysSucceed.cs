using InEngine.Core;

namespace InEngine.Commands
{
    /// <summary>
    /// Dummy command for testing and sample code.
    /// </summary>
    public class AlwaysSucceed : AbstractCommand
    {
        public override void Run()
        {
            Info("This command always succeeds.");
        }
    }
}
