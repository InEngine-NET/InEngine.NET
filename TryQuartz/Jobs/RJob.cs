using System;
using Quartz;
using RDotNet;
using RabbitMQ.Client;
using System.Text;
using Funq;

namespace TryQuartz.Jobs
{
    public class RJob : IJob
    {
        public IMessageQueueClient MessageQueueClient { get; set; }

        public RJob()
        {
            MessageQueueClient = ContainerSingleton.GetContainer().Resolve<IMessageQueueClient>();
        }

        public void Execute(IJobExecutionContext context)
        {
            MessageQueueClient.Publish("funq-e time");

            Console.WriteLine("Start R job!");

            //REngine.SetEnvironmentVariables();
            //var engine = REngine.GetInstance();
            //engine.Initialize();
//            engine.Dispose();
//            engine.Evaluate("source('sample.R')");
            //NumericVector group1 = engine.CreateNumericVector(new double[] { 30.02, 29.99, 30.11, 29.97, 30.01, 29.99 });
//            engine.SetSymbol("group1", group1);
            // Direct parsing from R script.
//            NumericVector group2 = engine.Evaluate("group2 <- c(29.89, 29.93, 29.72, 29.98, 30.02, 29.98)").AsNumeric();
            // Test difference of mean and get the P-value.
//            GenericVector testResult = engine.Evaluate("t.test(group1, group2)").AsList();
//            double p = testResult["p.value"].AsNumeric().First();

            Console.WriteLine("Finish R job!");
        }
    }
}
