using System;
using InEngine.Core.Exceptions;
using InEngine.Core.IO;

namespace InEngine.Core.LifeCycle;

public class CommandLifeCycle
{
    public MailSettings MailSettings { get; set; }
    public Action<AbstractCommand> BeforeAction { get; set; }
    public Action<AbstractCommand> AfterAction { get; set; }

    public bool ShouldPingBefore { get; set; }
    public string PingBeforeUrl { get; set; }

    public bool ShouldPingAfter { get; set; }
    public string PingAfterUrl { get; set; }

    public bool ShouldWriteOutputToFile { get; set; }
    public string WriteOutputToFilePath { get; set; }

    public bool ShouldAppendOutputToFile { get; set; }
    public string AppendOutputToFilePath { get; set; }

    public bool ShouldEmailOutput { get; set; }
    public string EmailOutputToAddress { get; set; }

    public int MaximumRetries { get; set; }
    public int CurrentTry { get; set; }

    public CommandLifeCycle IncrementRetry()
    {
        CurrentTry++;
        return this;
    }

    public bool ShouldRetry()
    {
        return CurrentTry <= MaximumRetries;
    }

    public void FirePreActions(AbstractCommand command)
    {
        try
        {
            if (ShouldPingBefore)
                IO.Http.Get(PingBeforeUrl);
        }
        catch (Exception exception)
        {
            throw new LifecycleActionFailedException($"Ping to {PingBeforeUrl} failed before running command.",
                exception);
        }

        try
        {
            BeforeAction?.Invoke(command);
        }
        catch (Exception exception)
        {
            throw new LifecycleActionFailedException($"Failed to invoke action after command ran successfully.",
                exception);
        }
    }

    public void FirePostActions(AbstractCommand command)
    {
        try
        {
            AfterAction?.Invoke(command);
        }
        catch (Exception exception)
        {
            throw new LifecycleActionFailedException($"Failed to invoke action after command ran successfully.",
                exception);
        }

        try
        {
            if (ShouldPingAfter)
                IO.Http.Get(PingAfterUrl);
        }
        catch (Exception exception)
        {
            throw new LifecycleActionFailedException(
                $"Ping failed for {PingAfterUrl} after command ran successfully.", exception);
        }

        var emailSubject = $"Command completed: {command.Name}";
        var commandOutput = command.FlushBuffer();
        try
        {
            if (ShouldEmailOutput)
                new Mail()
                {
                    Host = MailSettings.Host,
                    Port = MailSettings.Port,
                    Username = MailSettings.Username,
                    Password = MailSettings.Password,
                }.Send(MailSettings.From, EmailOutputToAddress, emailSubject, commandOutput);
        }
        catch (Exception exception)
        {
            throw new LifecycleActionFailedException(
                $"Email failed to send from {MailSettings.From} to {EmailOutputToAddress} with subject: \"{emailSubject}\"",
                exception
            );
        }

        try
        {
            if (ShouldWriteOutputToFile)
                command.Write.ToFile(WriteOutputToFilePath, commandOutput);
        }
        catch (Exception exception)
        {
            throw new LifecycleActionFailedException(
                $"Could not write text, of length {commandOutput.Length}, to path: {WriteOutputToFilePath}",
                exception
            );
        }

        try
        {
            if (ShouldAppendOutputToFile)
                command.Write.ToFile(AppendOutputToFilePath, commandOutput, true);
        }
        catch (Exception exception)
        {
            throw new LifecycleActionFailedException(
                $"Could not write text, of length {commandOutput.Length}, to path: {AppendOutputToFilePath}",
                exception
            );
        }
    }
}