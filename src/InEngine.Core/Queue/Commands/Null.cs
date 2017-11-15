using System;
namespace InEngine.Core.Queue.Commands
{
    public class Null : AbstractCommand
    {
        public override CommandResult Run()
        {
            return new CommandResult(true);
        }
    }
}
