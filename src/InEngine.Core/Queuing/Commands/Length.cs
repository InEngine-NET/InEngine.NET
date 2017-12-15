using System.Linq;

namespace InEngine.Core.Queuing.Commands
{
    public class Length : AbstractCommand
    {
        public override void Run()
        {
            PrintQueueLengths(QueueAdapter.Make());
            PrintQueueLengths(QueueAdapter.Make(true));
        }

        public void PrintQueueLengths(QueueAdapter queue)
        {
            Warning($"{queue.QueueName} Queue:");
            queue.GetQueueLengths().ToList().ForEach(x => { 
                InfoText(x.Key.PadLeft(15));
                Line(x.Value.ToString().PadLeft(10));
            });
            Newline();
        }
    }
}