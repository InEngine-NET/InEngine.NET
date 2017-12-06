using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using InEngine.Core.Exceptions;
using InEngine.Core.Queuing.Message;
using Konsole.Forms;

namespace InEngine.Core.Queuing.Commands
{
    public class Peek : AbstractCommand
    {
        [Option("from", DefaultValue = 0, HelpText = "The first commandEnvelope to peek at (0-indexed).")]
        public long From { get; set; } = 0;

        [Option("to", DefaultValue = 10, HelpText = "The last commandEnvelope to peek at.")]
        public long To { get; set; } = 10;

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

        public override void Run()
        {
            if (From < 0)
                throw new ArgumentException("--from cannot be negative");
            if (To < 0)
                throw new ArgumentException("--to cannot be negative");
            if (To < From)
                throw new ArgumentException("--from cannot be greater than --to");
            
            if (PendingQueue == false && FailedQueue == false && InProgressQueue == false)
                throw new CommandFailedException("Must specify at least one queue to peek in. Use -h to see available options.");
            var queue = QueueAdapter.Make(UseSecondaryQueue);
            if (PendingQueue) {
                PrintMessages(queue.PeekPendingMessages(From, To), "Pending");
            }
            if (InProgressQueue) {
                PrintMessages(queue.PeekInProgressMessages(From, To), "In-progress");
            }
            if (FailedQueue) {
                PrintMessages(queue.PeekFailedMessages(From, To), "Failed");
            }
        }

        public void PrintMessages(List<ICommandEnvelope> messages, string queueName)
        {
            WarningText($"{queueName}:");
            if (!messages.Any()) {
                Line(" no messages available.");
            }
         
            Newline();

            var konsoleForm = new Form(120, new ThinBoxStyle());
            messages.ForEach(x => {
                var commandEnvelope = x as ICommandEnvelope;
                if (JsonFormat)
                    Line(commandEnvelope.SerializeToJson());
                else
                    konsoleForm.Write(QueueAdapter.ExtractCommandInstanceFromMessage(commandEnvelope));
            });
        }
    }
}
