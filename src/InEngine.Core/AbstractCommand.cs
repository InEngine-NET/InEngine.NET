using System;
using Konsole;
using NLog;
using Quartz;

namespace InEngine.Core
{
    abstract public class AbstractCommand : ICommand, IJob
    {
        public IJobExecutionContext JobExecutionContext { get; set; }
        public ILogger Logger { get; internal set; }
        public ProgressBar ProgressBar { get; internal set; }
        string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                Logger = LogManager.GetLogger(_name);
            }
        }
        public string SchedulerGroup { get; set; }
        public string ScheduleId { get; set; }

        protected AbstractCommand()
        {
            ScheduleId = Guid.NewGuid().ToString();
            Name = GetType().FullName;
            SchedulerGroup = GetType().AssemblyQualifiedName;
        }

        public virtual CommandResult Run()
        {
            throw new NotImplementedException();
        }

        #region ProgressBar
        public void SetProgressBarMaxTicks(int maxTicks)
        {
            ProgressBar = new ProgressBar(maxTicks);
        }

        public void UpdateProgress(int tick)
        {
            ProgressBar.Refresh(tick, Name);
        }
        #endregion

        #region Scheduling
        public void Execute(IJobExecutionContext context)
        {
            JobExecutionContext = context;
            Run();
        }

        public T GetJobContextData<T>(string key)
        {
            if (JobExecutionContext == null || JobExecutionContext.MergedJobDataMap == null)
                return default(T);
            var objectVal = JobExecutionContext.MergedJobDataMap.Get(key);
            return objectVal == null ? default(T) : (T)objectVal;
        }

        public JobBuilder MakeJobBuilder()
        {
            return JobBuilder
                .Create(GetType())
                .WithIdentity($"{Name}:job:{ScheduleId}", SchedulerGroup);
        }

        public TriggerBuilder MakeTriggerBuilder()
        {
            return TriggerBuilder
                .Create()
                .WithIdentity($"{Name}:trigger:{ScheduleId}", SchedulerGroup);
        }
        #endregion
    }
}
