using System;
using System.Linq;
using System.Threading.Tasks;
using InEngine.Core.IO;
using InEngine.Core.Scheduling;
using Konsole;
using Quartz;

namespace InEngine.Core
{
    abstract public class AbstractCommand : ICommand, IFailed, IJob, IWrite
    {
        public ExecutionLifeCycle ExecutionLifeCycle { get; set; }
        public Write Write { get; set; }
        public ProgressBar ProgressBar { get; internal set; }
        public string Name { get; set; }
        public string SchedulerGroup { get; set; }
        public string ScheduleId { get; set; }
        public int SecondsBeforeTimeout { get; set; }

        protected AbstractCommand()
        {
            ScheduleId = Guid.NewGuid().ToString();
            Name = GetType().FullName;
            SchedulerGroup = GetType().AssemblyQualifiedName;
            Write = new Write();
            ExecutionLifeCycle = new ExecutionLifeCycle();
            SecondsBeforeTimeout = 300;
        }

        public virtual void Run()
        {
            throw new NotImplementedException();
        }

        public virtual void Failed(Exception exception)
        {}

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
        public virtual void Execute(IJobExecutionContext context)
        {
            if (context != null) {
                var properties = GetType().GetProperties();
                context.MergedJobDataMap.ToList().ForEach(x => {
                    var property = properties.FirstOrDefault(y => y.Name == x.Key);
                    if (property != null)
                        property.SetValue(this, x.Value);                
                });
            }

            try
            {
                ExecutionLifeCycle.FirePreActions(this);
                if (SecondsBeforeTimeout <= 0)
                    Run();
                else {
                    var task = Task.Run(() => Run());
                    if (!task.Wait(TimeSpan.FromSeconds(SecondsBeforeTimeout)))
                        throw new Exception($"Scheduled command timed out after {SecondsBeforeTimeout} second(s).");   
                }
                ExecutionLifeCycle.FirePostActions(this);
            }
            catch (Exception exception)
            {
                Failed(exception);
            }
        }
        #endregion

        #region Console output
        public IWrite Info(string val)
        {
            return Write.Info(val);
        }

        public IWrite Warning(string val)
        {
            return Write.Warning(val);
        }

        public IWrite Error(string val)
        {
            return Write.Error(val);
        }

        public IWrite Line(string val)
        {
            return Write.Line(val);
        }

        public IWrite ColoredLine(string val, ConsoleColor consoleColor)
        {
            return Write.ColoredLine(val, consoleColor);
        }

        public IWrite InfoText(string val)
        {
            return Write.InfoText(val);
        }

        public IWrite WarningText(string val)
        {
            return Write.WarningText(val);
        }

        public IWrite ErrorText(string val)
        {
            return Write.ErrorText(val);
        }

        public IWrite Text(string val)
        {
            return Write.Text(val);
        }

        public IWrite ColoredText(string val, ConsoleColor consoleColor)
        {
            return Write.ColoredText(val, consoleColor);
        }

        public IWrite Newline(int count = 1)
        {
            return Write.Newline(count);
        }

        public string FlushBuffer()
        {
            return Write.FlushBuffer();
        }

        public void ToFile(string path, string text, bool shouldAppend = false)
        {
            Write.ToFile(path, text, shouldAppend);
        }
        #endregion
    }
}
