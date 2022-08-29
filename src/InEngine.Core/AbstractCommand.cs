using System;
using System.Linq;
using System.Threading.Tasks;
using InEngine.Core.Exceptions;
using InEngine.Core.IO;
using InEngine.Core.LifeCycle;
using Konsole;
using Quartz;

namespace InEngine.Core;

using System.Threading;
using Microsoft.Extensions.Logging;

public abstract class AbstractCommand : IJob, IConsoleWrite, IHasCommandLifeCycle, IHasMailSettings
{
    protected readonly ILogger<AbstractCommand> Log;

    public CommandLifeCycle CommandLifeCycle { get; set; } = new CommandLifeCycle();
    public Write Write { get; set; } = new Write();
    public ProgressBar ProgressBar { get; internal set; }
    public string Name { get; set; }
    public string SchedulerGroup { get; set; }
    public string ScheduleId { get; set; }
    public int SecondsBeforeTimeout { get; set; } = 300;

    private MailSettings mailSettings;

    public MailSettings MailSettings
    {
        get => mailSettings;
        set => CommandLifeCycle.MailSettings = mailSettings = value;
    }

    protected AbstractCommand()
    {
        Log = LogManager.GetLogger<AbstractCommand>();
        ScheduleId = Guid.NewGuid().ToString();
        Name = GetType().FullName;
        SchedulerGroup = GetType().AssemblyQualifiedName;
    }

    public virtual void Run()
    {
    }

    public virtual async Task RunAsync()
    {
        Run();
        await Task.CompletedTask;
    }

    public virtual async Task RunWithLifeCycle()
    {
        try
        {
            CommandLifeCycle.FirePreActions(this);
            if (SecondsBeforeTimeout <= 0)
                await RunAsync();
            else
            {
                var timeoutSignal = new CancellationTokenSource(TimeSpan.FromSeconds(SecondsBeforeTimeout));
                try
                {
                    await RunAsync().WaitAsync(timeoutSignal.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw new CommandFailedException(
                        $"Scheduled command timed out after {SecondsBeforeTimeout} second(s).");
                }
            }

            CommandLifeCycle.FirePostActions(this);
        }
        catch (Exception exception)
        {
            const string message = "Command failed";
            Log.LogError(exception, message);
            Failed(exception);
            throw new CommandFailedException(message, exception);
        }
    }

    public virtual void Failed(Exception exception)
    {
    }

    #region ProgressBar

    public void SetProgressBarMaxTicks(int maxTicks) => ProgressBar = new ProgressBar(maxTicks);

    public void UpdateProgress(int tick) => ProgressBar.Refresh(tick, Name);

    #endregion

    #region Scheduling

    public virtual async Task Execute(IJobExecutionContext context)
    {
        if (context != null)
        {
            var properties = GetType().GetProperties();
            context.MergedJobDataMap.ToList().ForEach(x =>
            {
                var property = properties.FirstOrDefault(y => y.Name == x.Key);
                if (property != null)
                    property.SetValue(this, x.Value);
            });
        }

        await RunWithLifeCycle();
    }

    #endregion

    #region Console Output

    public IConsoleWrite Info(object val) => Write.Info(val);
    public IConsoleWrite Warning(object val) => Write.Warning(val);
    public IConsoleWrite Error(object val) => Write.Error(val);
    public IConsoleWrite Line(object val) => Write.Line(val);
    public IConsoleWrite LineWithColor(object val, ConsoleColor consoleColor) => Write.LineWithColor(val, consoleColor);
    public IConsoleWrite InfoText(object val) => Write.InfoText(val);
    public IConsoleWrite WarningText(object val) => Write.WarningText(val);
    public IConsoleWrite ErrorText(object val) => Write.ErrorText(val);
    public IConsoleWrite Text(object val) => Write.Text(val);

    public IConsoleWrite TextWithColor(object val, ConsoleColor consoleColor, bool writeLine) =>
        Write.TextWithColor(val, consoleColor, writeLine);

    public async Task NewlineAsync(int count = 1) => await Write.NewlineAsync(count);
    public async Task InfoAsync(object val) => await Write.InfoAsync(val);
    public async Task WarningAsync(object val) => await Write.WarningAsync(val);
    public async Task ErrorAsync(object val) => await Write.ErrorAsync(val);
    public async Task LineAsync(object val) => await Write.LineAsync(val);

    public async Task LineWithColorAsync(object val, ConsoleColor consoleColor) =>
        await Write.LineWithColorAsync(val, consoleColor);

    public async Task InfoTextAsync(object val) => await Write.InfoTextAsync(val);
    public async Task WarningTextAsync(object val) => await Write.WarningTextAsync(val);
    public async Task ErrorTextAsync(object val) => await Write.ErrorTextAsync(val);
    public async Task TextAsync(object val) => await Write.TextAsync(val);

    public async Task TextWithColorAsync(object val, ConsoleColor consoleColor, bool writeLine = false) =>
        await Write.TextWithColorAsync(val, consoleColor, writeLine);

    public IConsoleWrite Newline(int count = 1) => Write.Newline(count);
    public string FlushBuffer() => Write.FlushBuffer();
    public void ToFile(string path, string text, bool shouldAppend = false) => Write.ToFile(path, text, shouldAppend);

    #endregion
}