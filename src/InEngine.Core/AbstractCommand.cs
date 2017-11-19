using Konsole;
using NLog;
using Quartz;

namespace InEngine.Core
{
    abstract public class AbstractCommand : ICommand, IJob
    {
        public IJobExecutionContext JobExecutionContext { get; set; }
        public Logger Logger { get; internal set; }
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

        protected AbstractCommand()
        {
            Name = GetType().FullName;
        }

        public virtual CommandResult Run()
        {
            throw new System.NotImplementedException();
        }

        public void SetProgressBarMaxTicks(int maxTicks)
        {
            ProgressBar = new ProgressBar(maxTicks);
        }

        public void UpdateProgress(int tick)
        {
            ProgressBar.Refresh(tick, Name);   
        }

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
            return  objectVal == null ? default(T) : (T)objectVal;
        }
    }
}
