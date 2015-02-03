using IntegrationEngine.Model;
using System;
using System.Collections.Generic;
using System.Net;

namespace IntegrationEngine.Client
{
    public interface IInEngineClient
    {
        IJsonConvert JsonConvert { get; set; }
        HttpStatusCode Ping();
        IList<TItem> GetCollection<TItem>() where TItem : class;
        TItem Get<TItem>(string id) where TItem : class, IHasStringId;
        TItem Create<TItem>(TItem item);
        TItem Update<TItem>(TItem item) where TItem : class, IHasStringId;
        TItem Delete<TItem>(string id);
        IList<CronTrigger> GetCronTriggers();
        CronTrigger GetCronTriggerById(string id);
        CronTrigger CreateCronTrigger(CronTrigger cronTrigger);
        CronTrigger UpdateCronTrigger(CronTrigger cronTrigger);
        CronTrigger DeleteCronTrigger(string id);
        IList<SimpleTrigger> GetSimpleTriggers();
        SimpleTrigger GetSimpleTriggerById(string id);
        SimpleTrigger CreateSimpleTrigger(SimpleTrigger simpleTrigger);
        SimpleTrigger UpdateSimpleTrigger(SimpleTrigger simpleTrigger);
        SimpleTrigger DeleteSimpleTrigger(string id);
        IList<LogEvent> GetLogEvents();
        IList<JobType> GetJobTypes();
        HealthStatus GetHealthStatus();
    }
}

