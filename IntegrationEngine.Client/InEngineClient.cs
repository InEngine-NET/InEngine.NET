using InEngineLogEvent = IntegrationEngine.Model.LogEvent;
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
        /// Gets or sets the json convert.
        /// </summary>
        /// <value>The json convert.</value>
        public IJsonConvert JsonConvert { get; set; }

        /// <summary>
        /// Gets or sets the rest client.
        /// </summary>
        /// <value>The rest client.</value>
        public IRestClient RestClient { get; set; }

        /// <summary>
        /// Gets or sets the API URL.
        /// </summary>
        /// <value>The API URL.</value>
        public Uri ApiUrl { 
            get { return RestClient.BaseUrl; } 
            set { RestClient.BaseUrl = value; }
        }

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
            JsonConvert = new JsonConvertAdapter();
        }

        /// <summary>
        /// Ping the server.
        /// </summary>
        public HttpStatusCode Ping()
        {
            return RestClient.Execute(new RestRequest(typeof(HealthStatus).Name, Method.GET)).StatusCode;
        }

        public IList<TItem> GetCollection<TItem>() where TItem : class
        {
            var request = new RestRequest(typeof(TItem).Name, Method.GET);
            request.RequestFormat = DataFormat.Json;
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<IList<TItem>>(result.Content);
        }

        public TItem Get<TItem>(string id) where TItem : class, IHasStringId
        {
            var request = new RestRequest(typeof(TItem).Name + "/{id}", Method.GET);
            request.AddUrlSegment("id", id);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<TItem>(result.Content);
        }

        public TItem Create<TItem>(TItem item)
        {
            var request = new RestRequest(typeof(TItem).Name, Method.POST);
            var json = JsonConvert.SerializeObject(item);
            request.AddParameter("text/json", json, ParameterType.RequestBody);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<TItem>(result.Content);
        }

        public TItem Update<TItem>(TItem item) where TItem : class, IHasStringId
        {
            var request = new RestRequest(typeof(TItem).Name + "/{id}", Method.PUT);
            request.AddUrlSegment("id", item.Id);
            var json = JsonConvert.SerializeObject(item);
            request.AddParameter("text/json", json, ParameterType.RequestBody);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<TItem>(result.Content);
        }

        public TItem Delete<TItem>(string id)
        {
            var request = new RestRequest(typeof(TItem).Name + "/{id}", Method.DELETE);
            request.AddUrlSegment("id", id);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<TItem>(result.Content);
        }

        #region CronTrigger
        /// <summary>
        /// Gets the cron triggers.
        /// </summary>
        /// <returns>The cron triggers.</returns>
        public IList<CronTrigger> GetCronTriggers()
        {
            return GetCollection<CronTrigger>();
        }

        /// <summary>
        /// Gets the cron trigger by identifier.
        /// </summary>
        /// <returns>The cron trigger by identifier.</returns>
        /// <param name="id">Identifier.</param>
        public CronTrigger GetCronTriggerById(string id)
        {
            return Get<CronTrigger>(id);
        }

        /// <summary>
        /// Creates the cron trigger.
        /// </summary>
        /// <returns>The cron trigger.</returns>
        /// <param name="cronTrigger">Cron trigger.</param>
        public CronTrigger CreateCronTrigger(CronTrigger cronTrigger)
        {
            return Create(cronTrigger);
        }

        /// <summary>
        /// Updates the cron trigger.
        /// </summary>
        /// <returns>The cron trigger.</returns>
        /// <param name="cronTrigger">Cron trigger.</param>
        public CronTrigger UpdateCronTrigger(CronTrigger cronTrigger)
        {
            return Update(cronTrigger);
        }

        /// <summary>
        /// Deletes the cron trigger.
        /// </summary>
        /// <returns>The cron trigger.</returns>
        /// <param name="id">Identifier.</param>
        public CronTrigger DeleteCronTrigger(string id)
        {
            return Delete<CronTrigger>(id);
        }
        #endregion

        #region SimpleTrigger
        /// <summary>
        /// Gets the simple triggers.
        /// </summary>
        /// <returns>The simple triggers.</returns>
        public IList<SimpleTrigger> GetSimpleTriggers()
        {
            return GetCollection<SimpleTrigger>();
        }

        /// <summary>
        /// Gets the simple trigger by identifier.
        /// </summary>
        /// <returns>The simple trigger by identifier.</returns>
        /// <param name="id">Identifier.</param>
        public SimpleTrigger GetSimpleTriggerById(string id)
        {
            return Get<SimpleTrigger>(id);
        }

        /// <summary>
        /// Creates the simple trigger.
        /// </summary>
        /// <returns>The simple trigger.</returns>
        /// <param name="simpleTrigger">Simple trigger.</param>
        public SimpleTrigger CreateSimpleTrigger(SimpleTrigger simpleTrigger)
        {
            return Create(simpleTrigger);
        }


        /// <summary>
        /// Updates the simple trigger.
        /// </summary>
        /// <returns>The simple trigger.</returns>
        /// <param name="simpleTrigger">Simple trigger.</param>
        public SimpleTrigger UpdateSimpleTrigger(SimpleTrigger simpleTrigger)
        {
            return Update(simpleTrigger);
        }

        /// <summary>
        /// Deletes the simple trigger.
        /// </summary>
        /// <returns>The simple trigger.</returns>
        /// <param name="id">Identifier.</param>
        public SimpleTrigger DeleteSimpleTrigger(string id)
        {
            return Delete<SimpleTrigger>(id);
        }
        #endregion

        #region LogEvent
        /// <summary>
        /// Gets the log events.
        /// </summary>
        /// <returns>The log events.</returns>
        public IList<LogEvent> GetLogEvents()
        {
            return GetCollection<LogEvent>();
        }
        #endregion

        #region JobType
        /// <summary>
        /// Gets the job types.
        /// </summary>
        /// <returns>The job types.</returns>
        public IList<JobType> GetJobTypes()
        {
            return GetCollection<JobType>();
        }
        #endregion

        #region HealthStatus
        /// <summary>
        /// Gets the health status.
        /// </summary>
        /// <returns>The health status.</returns>
        public HealthStatus GetHealthStatus()
        {
            var request = new RestRequest(typeof(HealthStatus).Name, Method.GET);
            var result = RestClient.Execute(request);
            return JsonConvert.DeserializeObject<HealthStatus>(result.Content);
        }
        #endregion
    }
}

