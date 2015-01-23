using InEngineTimeZone = IntegrationEngine.Model.TimeZone;
using IntegrationEngine.Model;
using System;
using System.Collections.Generic;
using System.Net;

namespace IntegrationEngine.Client
{
    public interface IInEngineClient
    {
        HttpStatusCode Ping();
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
        IList<InEngineTimeZone> GetTimeZones();
        IList<string> GetJobTypes();
        HealthStatus GetHealthStatus();
    }
}

