using System;
using CommandLine;

namespace InEngine.Core.Queue.Commands
{
    public class Length : AbstractCommand
    {
        public override void Run()
        {
            var broker = Broker.Make();
            var leftPadding = 15;
            Warning("Primary Queue:");
            InfoText("Pending".PadLeft(leftPadding));
            Line(broker.GetPrimaryWaitingQueueLength().ToString().PadLeft(10));
            InfoText("In-progress".PadLeft(leftPadding));
            Line(broker.GetPrimaryProcessingQueueLength().ToString().PadLeft(10));
            ErrorText("Failed".PadLeft(leftPadding));
            Line(broker.GetPrimaryProcessingQueueLength().ToString().PadLeft(10));
            Newline();

            Warning("Secondary Queue:");
            InfoText("Pending".PadLeft(leftPadding));
            Line(broker.GetSecondaryWaitingQueueLength().ToString().PadLeft(10));
            InfoText("In-progress".PadLeft(leftPadding));
            Line(broker.GetSecondaryProcessingQueueLength().ToString().PadLeft(10));
            ErrorText("Failed".PadLeft(leftPadding));
            Line(broker.GetSecondaryProcessingQueueLength().ToString().PadLeft(10));
            Newline();
        }
    }
}
