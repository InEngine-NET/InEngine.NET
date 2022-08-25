using System.Threading;

namespace InEngine.Core.Commands
{
    public class Sleep : AbstractCommand
    {
        public int MillisecondsTimeout { get; set; }

        public override void Run()
        {
            Warning("Going to sleep...");
            Thread.Sleep(MillisecondsTimeout);
            Info("Done sleeping!");
        }
    }
}