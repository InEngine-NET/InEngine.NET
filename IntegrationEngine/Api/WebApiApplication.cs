using IntegrationEngine.Configuration;
using Microsoft.Owin.Hosting;
using System;

namespace IntegrationEngine.Api
{
    public class WebApiApplication : IWebApiApplication
    {
        public IDisposable webApi { get; set; }
        public WebApiConfiguration WebApiConfiguration { get; set; }

        public WebApiApplication()
        { }

        public void Start()
        {
            var baseAddress = (new UriBuilder("http", WebApiConfiguration.HostName, WebApiConfiguration.Port)).Uri.AbsoluteUri;
            webApi = WebApp.Start<WebApiStartup>(baseAddress);
        }

        public void Dispose()
        {
            webApi.Dispose();
        }
    }
}
