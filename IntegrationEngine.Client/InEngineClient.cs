using InEngineTimeZone = IntegrationEngine.Model.TimeZone;
using System;
using System.Collections.Generic;
using System.Linq;
using IntegrationEngine.Model;
using RestSharp;
using Newtonsoft.Json;

namespace IntegrationEngine.Client
{
    public class InEngineClient
    {
        public RestClient RestClient { get; set; }

        public InEngineClient() 
            : this("http://localhost:9001/api/")
        {
        }

        public InEngineClient(string apiUrl)
        {
            RestClient = new RestClient(apiUrl);
        }

        #region CronTrigger
        public IList<CronTrigger> GetCronTriggers()
        {
            var request = new RestRequest(EndpointName.CronTrigger, Method.GET);
            request.RequestFormat = DataFormat.Json;
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<IList<CronTrigger>>(result.Content);
        }

        public CronTrigger GetCronTriggerById(string id)
        {
            var request = new RestRequest(EndpointName.CronTrigger + "/{id}", Method.GET);
            request.AddUrlSegment("id", id);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<CronTrigger>(result.Content);
        }

        public CronTrigger CreateCronTrigger(CronTrigger cronTrigger)
        {
            var request = new RestRequest(EndpointName.CronTrigger, Method.POST);
            request.AddObject(cronTrigger);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<CronTrigger>(result.Content);
        }

        public CronTrigger UpdateCronTrigger(CronTrigger cronTrigger)
        {
            var request = new RestRequest(EndpointName.CronTrigger + "/{id}", Method.PUT);
            request.AddUrlSegment("id", cronTrigger.Id);
            request.AddObject(cronTrigger);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<CronTrigger>(result.Content);
        }

        public CronTrigger DeleteCronTrigger(string id)
        {
            var request = new RestRequest(EndpointName.CronTrigger + "/{id}", Method.DELETE);
            request.AddUrlSegment("id", id);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<CronTrigger>(result.Content);
        }
        #endregion

        #region SimpleTrigger
        public IList<SimpleTrigger> GetSimpleTriggers()
        {
            var request = new RestRequest(EndpointName.SimpleTrigger, Method.GET);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<IList<SimpleTrigger>>(result.Content);
        }

        public SimpleTrigger GetSimpleTriggerById(string id)
        {
            var request = new RestRequest(EndpointName.SimpleTrigger + "/{id}", Method.GET);
            request.AddUrlSegment("id", id);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<SimpleTrigger>(result.Content);
        }

        public SimpleTrigger CreateSimpleTrigger(SimpleTrigger simpleTrigger)
        {
            var request = new RestRequest(EndpointName.SimpleTrigger, Method.POST);
            request.AddObject(simpleTrigger);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<SimpleTrigger>(result.Content);
        }

        public SimpleTrigger UpdateSimpleTrigger(SimpleTrigger simpleTrigger)
        {
            var request = new RestRequest(EndpointName.SimpleTrigger + "/{id}", Method.PUT);
            request.AddUrlSegment("id", simpleTrigger.Id);
            request.AddObject(simpleTrigger);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<SimpleTrigger>(result.Content);
        }

        public SimpleTrigger DeleteSimpleTrigger(string id)
        {
            var request = new RestRequest(EndpointName.SimpleTrigger + "/{id}", Method.DELETE);
            request.AddUrlSegment("id", id);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<SimpleTrigger>(result.Content);
        }
        #endregion

        #region TimeZone
        public IList<InEngineTimeZone> GetTimeZones()
        {
            var request = new RestRequest(EndpointName.TimeZone, Method.GET);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<IList<InEngineTimeZone>>(result.Content);
        }
        #endregion

        #region JobType
        public IList<string> GetJobTypes()
        {
            var request = new RestRequest(EndpointName.JobType, Method.GET);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<IList<string>>(result.Content);
        }
        #endregion

        #region HealthStatus
        public IList<HealthStatus> GetHealthStatus()
        {
            var request = new RestRequest(EndpointName.HealthStatus, Method.GET);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<HealthStatus>(result.Content);
        }
        #endregion
    }
}

