using System;

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
            var textLeftPadding = 15;
            var numberLeftPadding = 2;
            Warning($"{queue.QueueName} Queue:");

            try
            {
                InfoText("".PadLeft(numberLeftPadding));
                InfoText("Pending");
                InfoText("".PadLeft(textLeftPadding));
                Line(queue.GetPendingQueueLength().ToString());
            }
            catch (NotImplementedException)
            {
                Error("Not supported by queue client.");
            }

            try
            {
                InfoText("".PadLeft(numberLeftPadding));
                InfoText("In-progress");
                InfoText("".PadLeft(textLeftPadding));
                Line(queue.GetInProgressQueueLength().ToString());
            }
            catch (NotImplementedException)
            {
                Error("Not supported by queue client.");
            }

            try
            {
                InfoText("".PadLeft(numberLeftPadding));
                InfoText("Failed");
                InfoText("".PadLeft(textLeftPadding));
                Line(queue.GetFailedQueueLength().ToString());
            }
            catch (NotImplementedException)
            {
                Error("Not supported by queue client.");
            }
            Newline();
        }
    }
}
