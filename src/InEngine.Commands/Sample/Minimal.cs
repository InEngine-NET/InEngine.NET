using InEngine.Core;

namespace InEngine.Commands.Sample
{
    /*
     * At a minimum, a command must implement the ICommand interface.
     *
     * A command result must be returned from the Run method.
     * The command result has an optional message. This is especially helpful
     * when the command does not finish successfully.
     */
    public class Minimal : ICommand
    {
        public void Run()
        {
        }
    }
}
