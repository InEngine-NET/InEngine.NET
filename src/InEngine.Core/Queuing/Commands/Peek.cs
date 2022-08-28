using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using InEngine.Core.Exceptions;
using InEngine.Core.Queuing.Message;
using Konsole.Forms;
using System.Threading.Tasks;

namespace InEngine.Core.Queuing.Commands;

using Microsoft.Extensions.Logging;

public class Peek : AbstractCommand, IHasQueueSettings
{
    [Option("from", DefaultValue = 0, HelpText = "The first command to peek at (0-indexed).")]
    public long From { get; set; } = 0;

    [Option("to", DefaultValue = 9, HelpText = "The last command to peek at.")]
    public long To { get; set; } = 9;

    [Option("json", HelpText = "View the messages as JSON.")]
    public bool JsonFormat { get; set; }

    [Option("pending", HelpText = "Peek at messages in the pending queue.")]
    public bool PendingQueue { get; set; }

    [Option("failed", HelpText = "Peek at messages in the failed queue.")]
    public bool FailedQueue { get; set; }

    [Option("in-progress", HelpText = "Peek at messages in the in-progress queue.")]
    public bool InProgressQueue { get; set; }

    [Option("secondary", HelpText = "Peek at messages in secondary queues. Primary queues are used by default.")]
    public bool UseSecondaryQueue { get; set; }

    public QueueSettings QueueSettings { get; set; }

    public override async Task Run()
    {
        if (From < 0)
            throw new ArgumentException("--from cannot be negative");
        if (To < 0)
            throw new ArgumentException("--to cannot be negative");
        if (To < From)
            throw new ArgumentException("--from cannot be greater than --to");

        if (PendingQueue == false && FailedQueue == false && InProgressQueue == false)
            throw new CommandFailedException(
                "Must specify at least one queue to peek in. Use -h to see available options.");
        var queue = QueueAdapter.Make(UseSecondaryQueue, QueueSettings, MailSettings);

        try
        {
            if (PendingQueue)
                PrintMessages(queue.PeekPendingMessages(From, To), "Pending");
        }
        catch (Exception exception)
        {
            Log.LogWarning(exception, "Error while peeking at pending queue");
        }

        try
        {
            if (InProgressQueue)
                PrintMessages(queue.PeekInProgressMessages(From, To), "InProgress");
        }
        catch (Exception exception)
        {
            Log.LogWarning(exception, "Error while peeking at in-progress queue");
        }

        try
        {
            if (FailedQueue)
                PrintMessages(queue.PeekInProgressMessages(From, To), "Failed");
        }
        catch (Exception exception)
        {
            Log.LogWarning(exception, "Error while peeking at failed queue");
        }
    }

    public void PrintMessages(List<ICommandEnvelope> messages, string queueName)
    {
        WarningText($"{queueName}:");
        if (!messages.Any())
            Line(" no messages available.");

        Newline();

        var konsoleForm = new Form(120, new ThinBoxStyle());
        messages.ForEach(x =>
        {
            var commandEnvelope = x as ICommandEnvelope;
            if (JsonFormat)
                Line(commandEnvelope.SerializeToJson());
            else
                konsoleForm.Write(commandEnvelope.GetCommandInstanceAndIncrementRetry());
        });
    }
}