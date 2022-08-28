using System.Linq;
using System.Threading.Tasks;

namespace InEngine.Core.Queuing.Commands;

public class Status : AbstractCommand, IHasQueueSettings
{
    public QueueSettings QueueSettings { get; set; }

    public override async Task Run()
    {
        PrintQueueLengths(QueueAdapter.Make(false, QueueSettings, MailSettings));
        PrintQueueLengths(QueueAdapter.Make(true, QueueSettings, MailSettings));
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