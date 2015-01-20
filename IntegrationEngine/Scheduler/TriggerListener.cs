using Quartz;

namespace IntegrationEngine.Scheduler
{
    public class TriggerListener : ITriggerListener
    {
        public string Name { get { return this.GetType().FullName;} } 

        public void TriggerComplete(ITrigger trigger, IJobExecutionContext context, SchedulerInstruction triggerInstructionCode)
        {
        }

        public void TriggerFired(ITrigger trigger, IJobExecutionContext context)
        {
            
        }

        public void TriggerMisfired(ITrigger trigger)
        {
            
        }

        public bool VetoJobExecution(ITrigger trigger, IJobExecutionContext context)
        {
            return false;
        }
    }
}
