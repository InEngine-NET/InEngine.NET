using System;

namespace InEngine.Core.Commands
{
    public class Lambda : AbstractCommand
    {
        public Action Action { get; set; }
        public override void Run()
        {
            Action.Invoke();
        }
    }
}
