using System;
using Quartz;
using IntegrationEngine.Jobs;
using IntegrationEngine.Reports;

namespace IntegrationEngine
{
    public class CarReportJob : AsyncJob, IAnalysisJob
    {
        public CarReportJob()
        {}

        public override void Execute(IJobExecutionContext context)
        {
            MessageQueueClient.Publish(this);
        }

        public void RunAnalysis()
        {
            var rscriptRunner = ContainerSingleton.GetContainer().Resolve<RScriptRunner>();
            var report = new CarReport() {
                Created = DateTime.UtcNow
            };
            rscriptRunner.Run(report);
        }
    }
}
