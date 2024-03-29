﻿using System;

namespace InEngine.Core.Scheduling.LifeCycle;

public class ScheduleLifeCycleBuilder : IScheduleLifeCycleBuilder
{
    public AbstractCommand Command { 
        get { return JobRegistration.Command; }
        set { throw new NotImplementedException(); }
    }

    public JobRegistration JobRegistration { get; set; }


    public IScheduleLifeCycleBuilder Before(Action<AbstractCommand> action)
    {
        JobRegistration.Command.CommandLifeCycle.BeforeAction = action;
        return this;
    }

    public IScheduleLifeCycleBuilder After(Action<AbstractCommand> action)
    {
        JobRegistration.Command.CommandLifeCycle.AfterAction = action;
        return this;
    }

    public IScheduleLifeCycleBuilder PingBefore(string url)
    {
        JobRegistration.Command.CommandLifeCycle.ShouldPingBefore = true;
        JobRegistration.Command.CommandLifeCycle.PingBeforeUrl = url;
        return this;
    }

    public IScheduleLifeCycleBuilder PingAfter(string url)
    {
        JobRegistration.Command.CommandLifeCycle.ShouldPingAfter = true;
        JobRegistration.Command.CommandLifeCycle.PingAfterUrl = url;
        return this;
    }

    public IScheduleLifeCycleBuilder WriteOutputTo(string output)
    {
        JobRegistration.Command.CommandLifeCycle.ShouldWriteOutputToFile = true;
        JobRegistration.Command.CommandLifeCycle.WriteOutputToFilePath = output;
        return this;
    }

    public IScheduleLifeCycleBuilder AppendOutputTo(string output)
    {
        JobRegistration.Command.CommandLifeCycle.ShouldAppendOutputToFile = true;
        JobRegistration.Command.CommandLifeCycle.AppendOutputToFilePath = output;
        return this;
    }

    public IScheduleLifeCycleBuilder EmailOutputTo(string emailAddress)
    {
        JobRegistration.Command.CommandLifeCycle.ShouldEmailOutput = true;
        JobRegistration.Command.CommandLifeCycle.EmailOutputToAddress = emailAddress;
        return this;
    }
}