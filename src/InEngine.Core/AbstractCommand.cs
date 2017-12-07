using System;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;
using InEngine.Core.Exceptions;
using InEngine.Core.IO;
using InEngine.Core.LifeCycle;
using Konsole;
using Quartz;

namespace InEngine.Core
{
    abstract public class AbstractCommand : IJob, IWrite, IHasCommandLifeCycle
    {
        public CommandLifeCycle CommandLifeCycle { get; set; }
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
            CommandLifeCycle = new CommandLifeCycle();
            SecondsBeforeTimeout = 300;
        }

        public virtual void Run()
        {
            throw new NotImplementedException();
        }

        public virtual void RunWithLifeCycle()
        {
            try
            {
                CommandLifeCycle.FirePreActions(this);
                if (SecondsBeforeTimeout <= 0)
                    Run();
                else
                {
                    var task = Task.Run(() => Run());
                    if (!task.Wait(TimeSpan.FromSeconds(SecondsBeforeTimeout)))
                        throw new Exception($"Scheduled command timed out after {SecondsBeforeTimeout} second(s).");
                }
                CommandLifeCycle.FirePostActions(this);
            }
            catch (Exception exception)
            {
                Failed(exception);
                throw new CommandFailedException("Command failed. See inner exception for details.", exception);
            }
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
            if (context != null)
            {
                var properties = GetType().GetProperties();
                context.MergedJobDataMap.ToList().ForEach(x => {
                    var property = properties.FirstOrDefault(y => y.Name == x.Key);
                    if (property != null)
                        property.SetValue(this, x.Value);
                });
            }

            RunWithLifeCycle();
        }
        #endregion

        #region Console output
        public IWrite Info(object val)
        {
            return Write.Info(val);
        }

        public IWrite Warning(object val)
        {
            return Write.Warning(val);
        }

        public IWrite Error(object val)
        {
            return Write.Error(val);
        }

        public IWrite Line(object val)
        {
            return Write.Line(val);
        }

        public IWrite ColoredLine(object val, ConsoleColor consoleColor)
        {
            return Write.ColoredLine(val, consoleColor);
        }

        public IWrite InfoText(object val)
        {
            return Write.InfoText(val);
        }

        public IWrite WarningText(object val)
        {
            return Write.WarningText(val);
        }

        public IWrite ErrorText(object val)
        {
            return Write.ErrorText(val);
        }

        public IWrite Text(object val)
        {
            return Write.Text(val);
        }

        public IWrite ColoredText(object val, ConsoleColor consoleColor)
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
