using System;
using System.Collections.Generic;
using System.Linq;
using IntegrationEngine.Model;
using RestSharp;

namespace IntegrationEngine.Client
{
    public class Client
    {
        public RestClient RestClient { get; set; }

        public Client() 
            : this("http://localhost:9001/api/")
        {
        }

        public Client(string apiUrl)
        {
            RestClient = new RestClient(apiUrl);
        }

        #region CronTrigger
        public List<ICronTrigger> GetCronTriggers()
        {
            var request = new RestRequest(EndpointName.CronTrigger, Method.GET);
            return RestClient.Execute<List<ICronTrigger>>(request).Data;
        }

        public CronTrigger GetCronTriggerById(string id)
        {
            var request = new RestRequest(EndpointName.CronTrigger + "/{id}", Method.GET);
            request.AddUrlSegment("id", id);
            return RestClient.Execute<CronTrigger>(request).Data;
        }

        public CronTrigger CreateCronTrigger(CronTrigger cronTrigger)
        {
            var request = new RestRequest(EndpointName.CronTrigger, Method.POST);
            request.AddObject(cronTrigger);
            return RestClient.Execute<CronTrigger>(request).Data;
        }

        public CronTrigger UpdateCronTrigger(CronTrigger cronTrigger)
        {
            var request = new RestRequest(EndpointName.CronTrigger + "/{id}", Method.PUT);
            request.AddUrlSegment("id", cronTrigger.Id);
            request.AddObject(cronTrigger);
            return RestClient.Execute<CronTrigger>(request).Data;
        }

        public CronTrigger DeleteCronTrigger(string id)
        {
            var request = new RestRequest(EndpointName.CronTrigger + "/{id}", Method.DELETE);
            request.AddUrlSegment("id", id);
            return RestClient.Execute<CronTrigger>(request).Data;
        }
        #endregion

        #region SimpleTrigger
        public List<SimpleTrigger> GetSimpleTriggers()
        {
            var request = new RestRequest(EndpointName.SimpleTrigger, Method.GET);
            return RestClient.Execute<List<SimpleTrigger>>(request).Data;
        }

        public SimpleTrigger GetSimpleTriggerById(string id)
        {
            var request = new RestRequest(EndpointName.SimpleTrigger + "/{id}", Method.GET);
            request.AddUrlSegment("id", id);
            return RestClient.Execute<SimpleTrigger>(request).Data;
        }

        public SimpleTrigger CreateSimpleTrigger(SimpleTrigger simpleTrigger)
        {
            var request = new RestRequest(EndpointName.SimpleTrigger, Method.POST);
            request.AddObject(simpleTrigger);
            return RestClient.Execute<SimpleTrigger>(request).Data;
        }

        public SimpleTrigger UpdateSimpleTrigger(SimpleTrigger simpleTrigger)
        {
            var request = new RestRequest(EndpointName.SimpleTrigger + "/{id}", Method.PUT);
            request.AddUrlSegment("id", simpleTrigger.Id);
            request.AddObject(simpleTrigger);
            return RestClient.Execute<SimpleTrigger>(request).Data;
        }

        public SimpleTrigger DeleteSimpleTrigger(string id)
        {
            var request = new RestRequest(EndpointName.SimpleTrigger + "/{id}", Method.DELETE);
            request.AddUrlSegment("id", id);
            return RestClient.Execute<SimpleTrigger>(request).Data;
        }
        #endregion

        #region TimeZone
        public List<TimeZone> GetTimeZones()
        {
            var request = new RestRequest(EndpointName.TimeZone, Method.GET);
            return RestClient.Execute<List<TimeZone>>(request).Data;
        }
        #endregion

        #region JobType
        public List<string> GetJobTypes()
        {
            var request = new RestRequest(EndpointName.JobType, Method.GET);
            return RestClient.Execute<List<string>>(request).Data;
        }
        #endregion
    }
}

