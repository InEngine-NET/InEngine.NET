using System.Threading.Tasks;
using System.Linq;
using CommandLine;

namespace InEngine.Core.Queuing.Commands;

public class RepublishFailed : AbstractCommand, IHasQueueSettings
{
    [Option("limit", DefaultValue = 100, HelpText = "The maximum number of messages to republish.")]
    public int Limit { get; set; }

    [Option("secondary", DefaultValue = false, HelpText = "Republish failed secondary queue messages.")]
    public bool UseSecondaryQueue { get; set; }

    public QueueSettings QueueSettings { get; set; }

    public override async Task Run()
    {
        var queue = QueueAdapter.Make(UseSecondaryQueue, QueueSettings, MailSettings);
        Enumerable.Range(0, Limit)
            .ToList()
            .ForEach(x => queue.RepublishFailedMessages());
    }
}