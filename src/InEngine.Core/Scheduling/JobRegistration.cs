using Quartz;

namespace InEngine.Core.Scheduling
{
    public class JobRegistration
    {
        public AbstractCommand Command { get; set; }
        public IJobDetail JobDetail { get; set; }
        public ITrigger Trigger { get; set; }

        public JobRegistration()
        {}

        public JobRegistration(AbstractCommand command, IJobDetail jobDetail, ITrigger trigger)
            : this()
        {
            Command = command;
            JobDetail = jobDetail;
            Trigger = trigger;
        }
    }
}
