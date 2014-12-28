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
            var rscriptLauncher = ContainerSingleton.GetContainer().Resolve<RScriptLauncher>();
            var report = new CarReport() {
                Created = DateTime.UtcNow
            };
            rscriptLauncher.Run(report);
        }
    }
}
