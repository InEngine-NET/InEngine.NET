using InEngineTimeZone = IntegrationEngine.Model.TimeZone;
using System;
using System.Collections.Generic;
using System.Linq;
using IntegrationEngine.Model;
using RestSharp;
using Newtonsoft.Json;
using System.Net;

namespace IntegrationEngine.Client
{
    /// <summary>
    /// In engine client.
    /// </summary>
    public class InEngineClient : IInEngineClient
    {
        /// <summary>
        /// Gets or sets the rest client.
        /// </summary>
        /// <value>The rest client.</value>
        public RestClient RestClient { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationEngine.Client.InEngineClient"/> class.
        /// </summary>
        public InEngineClient() 
            : this("http://localhost:9001/api/")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntegrationEngine.Client.InEngineClient"/> class.
        /// </summary>
        /// <param name="apiUrl">API URL.</param>
        public InEngineClient(string apiUrl)
        {
            RestClient = new RestClient(apiUrl);
        }

        /// <summary>
        /// Ping the server.
        /// </summary>
        public HttpStatusCode Ping()
        {
            return RestClient.Execute(new RestRequest(EndpointName.HealthStatus, Method.GET)).StatusCode;
        }

        #region CronTrigger
        /// <summary>
        /// Gets the cron triggers.
        /// </summary>
        /// <returns>The cron triggers.</returns>
        public IList<CronTrigger> GetCronTriggers()
        {
            var request = new RestRequest(EndpointName.CronTrigger, Method.GET);
            request.RequestFormat = DataFormat.Json;
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<IList<CronTrigger>>(result.Content);
        }

        /// <summary>
        /// Gets the cron trigger by identifier.
        /// </summary>
        /// <returns>The cron trigger by identifier.</returns>
        /// <param name="id">Identifier.</param>
        public CronTrigger GetCronTriggerById(string id)
        {
            var request = new RestRequest(EndpointName.CronTrigger + "/{id}", Method.GET);
            request.AddUrlSegment("id", id);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<CronTrigger>(result.Content);
        }

        /// <summary>
        /// Creates the cron trigger.
        /// </summary>
        /// <returns>The cron trigger.</returns>
        /// <param name="cronTrigger">Cron trigger.</param>
        public CronTrigger CreateCronTrigger(CronTrigger cronTrigger)
        {
            var request = new RestRequest(EndpointName.CronTrigger, Method.POST);
            request.AddObject(cronTrigger);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<CronTrigger>(result.Content);
        }

        /// <summary>
        /// Updates the cron trigger.
        /// </summary>
        /// <returns>The cron trigger.</returns>
        /// <param name="cronTrigger">Cron trigger.</param>
        public CronTrigger UpdateCronTrigger(CronTrigger cronTrigger)
        {
            var request = new RestRequest(EndpointName.CronTrigger + "/{id}", Method.PUT);
            request.AddUrlSegment("id", cronTrigger.Id);
            request.AddObject(cronTrigger);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<CronTrigger>(result.Content);
        }

        /// <summary>
        /// Deletes the cron trigger.
        /// </summary>
        /// <returns>The cron trigger.</returns>
        /// <param name="id">Identifier.</param>
        public CronTrigger DeleteCronTrigger(string id)
        {
            var request = new RestRequest(EndpointName.CronTrigger + "/{id}", Method.DELETE);
            request.AddUrlSegment("id", id);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<CronTrigger>(result.Content);
        }
        #endregion

        #region SimpleTrigger
        /// <summary>
        /// Gets the simple triggers.
        /// </summary>
        /// <returns>The simple triggers.</returns>
        public IList<SimpleTrigger> GetSimpleTriggers()
        {
            var request = new RestRequest(EndpointName.SimpleTrigger, Method.GET);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<IList<SimpleTrigger>>(result.Content);
        }

        /// <summary>
        /// Gets the simple trigger by identifier.
        /// </summary>
        /// <returns>The simple trigger by identifier.</returns>
        /// <param name="id">Identifier.</param>
        public SimpleTrigger GetSimpleTriggerById(string id)
        {
            var request = new RestRequest(EndpointName.SimpleTrigger + "/{id}", Method.GET);
            request.AddUrlSegment("id", id);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<SimpleTrigger>(result.Content);
        }

        /// <summary>
        /// Creates the simple trigger.
        /// </summary>
        /// <returns>The simple trigger.</returns>
        /// <param name="simpleTrigger">Simple trigger.</param>
        public SimpleTrigger CreateSimpleTrigger(SimpleTrigger simpleTrigger)
        {
            var request = new RestRequest(EndpointName.SimpleTrigger, Method.POST);
            request.AddObject(simpleTrigger);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<SimpleTrigger>(result.Content);
        }


        /// <summary>
        /// Updates the simple trigger.
        /// </summary>
        /// <returns>The simple trigger.</returns>
        /// <param name="simpleTrigger">Simple trigger.</param>
        public SimpleTrigger UpdateSimpleTrigger(SimpleTrigger simpleTrigger)
        {
            var request = new RestRequest(EndpointName.SimpleTrigger + "/{id}", Method.PUT);
            request.AddUrlSegment("id", simpleTrigger.Id);
            request.AddObject(simpleTrigger);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<SimpleTrigger>(result.Content);
        }

        /// <summary>
        /// Deletes the simple trigger.
        /// </summary>
        /// <returns>The simple trigger.</returns>
        /// <param name="id">Identifier.</param>
        public SimpleTrigger DeleteSimpleTrigger(string id)
        {
            var request = new RestRequest(EndpointName.SimpleTrigger + "/{id}", Method.DELETE);
            request.AddUrlSegment("id", id);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<SimpleTrigger>(result.Content);
        }
        #endregion

        #region TimeZone
        /// <summary>
        /// Gets the time zones.
        /// </summary>
        /// <returns>The time zones.</returns>
        public IList<InEngineTimeZone> GetTimeZones()
        {
            var request = new RestRequest(EndpointName.TimeZone, Method.GET);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<IList<InEngineTimeZone>>(result.Content);
        }
        #endregion

        #region JobType
        /// <summary>
        /// Gets the job types.
        /// </summary>
        /// <returns>The job types.</returns>
        public IList<JobType> GetJobTypes()
        {
            var request = new RestRequest(EndpointName.JobType, Method.GET);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<IList<JobType>>(result.Content);
        }
        #endregion

        #region HealthStatus
        /// <summary>
        /// Gets the health status.
        /// </summary>
        /// <returns>The health status.</returns>
        public HealthStatus GetHealthStatus()
        {
            var request = new RestRequest(EndpointName.HealthStatus, Method.GET);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<HealthStatus>(result.Content);
        }
        #endregion
    }
}

