using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InEngine.Commands;
using InEngine.Core;
using InEngine.Core.Commands;
using InEngine.Core.Exceptions;
using InEngine.Core.Queuing;
using InEngine.Core.Queuing.Commands;              

namespace InEngine.IntegrationTest
{
    public class QueuingTest : AbstractCommand
    {
        public override void Run()
        {
            var settings = InEngineSettings.Make();
            var queue = QueueAdapter.Make(true, settings.Queue, settings.Mail);

            queue.ClearPendingQueue();
            queue.Publish(new Echo() { VerbatimText = "Core echo command." });
            new Length { 
                QueueSettings = settings.Queue,
                MailSettings = settings.Mail,
            }.Run();
            new Peek { 
                PendingQueue = true,
                QueueSettings = settings.Queue,
                MailSettings = settings.Mail,
            }.Run();
            var consume = new Consume { 
                Count = 1000,
                QueueSettings = settings.Queue,
                MailSettings = settings.Mail,
            };

            Enqueue.Command(() => Console.WriteLine("Core lambda command."))
                   .Dispatch();
            Enqueue.Command(() => new Echo { VerbatimText = "Core echo command in a lambda command." }.Run())
                   .Dispatch();
            Enqueue.Command(new AlwaysFail())
                   .WriteOutputTo("queueWriteTest-TheFileShouldNotExist.txt")
                   .WithRetries(4)
                   .Dispatch();

            Enqueue.Commands(new[] {
                new Echo { 
                    VerbatimText = "Chain Link 1",
                    MailSettings = settings.Mail,
                },
                new Echo { 
                    VerbatimText = "Chain Link 2",
                    MailSettings = settings.Mail,
                },
            }).Dispatch();

            Enqueue.Commands(new List<AbstractCommand> {
                new Echo { 
                    VerbatimText = "Chain Link A",
                    MailSettings = settings.Mail,
                },
                new AlwaysFail(),
                new Echo { 
                    VerbatimText = "Chain Link C",
                    MailSettings = settings.Mail,
                },
            }).Dispatch();

            Enqueue.Commands(new List<AbstractCommand> {
                new Echo { 
                    VerbatimText = "Chain Link A",
                    MailSettings = settings.Mail,
                },
                new AlwaysFail(),
                new Echo { 
                    VerbatimText = "Chain Link C",
                    MailSettings = settings.Mail,
                },
            }).Dispatch();

            Enqueue.Commands(Enumerable.Range(0, 10).Select(x => new AlwaysSucceed() as AbstractCommand).ToList())
                   .Dispatch();
            
            consume.Run();

            var queueWriteIntegrationTest = "queueWriteIntegrationTest.txt";
            var queueAppendIntegrationTest = "queueAppendIntegrationTest.txt";
            File.Delete(queueWriteIntegrationTest);
            File.Delete(queueAppendIntegrationTest);
            Enqueue.Command(new Echo { 
                VerbatimText = "Core echo command.",
                MailSettings = settings.Mail,
            })
                   .PingAfter("http://www.google.com")
                   .PingBefore("http://www.google.com")
                   .EmailOutputTo("example@inengine.net")
                   .WriteOutputTo(queueWriteIntegrationTest)
                   .AppendOutputTo(queueAppendIntegrationTest)
                   .Dispatch();

            consume.Run();
        }
    }
}
