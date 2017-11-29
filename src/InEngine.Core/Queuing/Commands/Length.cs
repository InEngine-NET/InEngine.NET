using System;
using CommandLine;

namespace InEngine.Core.Queuing.Commands
{
    public class Length : AbstractCommand
    {
        public override void Run()
        {
            PrintQueueLengths(Queue.Make());
            PrintQueueLengths(Queue.Make(true));
        }

        public void PrintQueueLengths(Queue queue)
        {
            var leftPadding = 15;
            Warning($"{queue.QueueName} Queue:");
            InfoText("Pending".PadLeft(leftPadding));
            Line(queue.GetPendingQueueLength().ToString().PadLeft(10));
            InfoText("In-progress".PadLeft(leftPadding));
            Line(queue.GetInProgressQueueLength().ToString().PadLeft(10));
            ErrorText("Failed".PadLeft(leftPadding));
            Line(queue.GetFailedQueueLength().ToString().PadLeft(10));
            Newline();
        }
    }
}
