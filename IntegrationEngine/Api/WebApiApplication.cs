using IntegrationEngine.Core.Configuration;
using Microsoft.Owin.Hosting;
using Microsoft.Practices.Unity;
using Owin;
using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

namespace IntegrationEngine.Api
{
    public class WebApiApplication : IWebApiApplication
    {
        public IDisposable webApi { get; set; }
        public WebApiConfiguration WebApiConfiguration { get; set; }
        public ContainerResolver ContainerResolver { get; set; }

        public WebApiApplication()
        {}

        public HttpConfiguration HttpConfigurationFactory()
        {
            var  config = new HttpConfiguration();
            config.DependencyResolver = ContainerResolver;
            if (WebApiConfiguration != null && WebApiConfiguration.Origins.Any())
                config.EnableCors(new EnableCorsAttribute(string.Join(",", WebApiConfiguration.Origins), "*", "*"));
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            config.MapHttpAttributeRoutes();
            return config;
        }

        public void Start()
        {
            var baseAddress = (new UriBuilder("http", WebApiConfiguration.HostName, WebApiConfiguration.Port)).Uri.AbsoluteUri;
            webApi = WebApp.Start(baseAddress, x => x.UseWebApi(HttpConfigurationFactory()));
        }

        public void Dispose()
        {
            if (webApi != null)
                webApi.Dispose();
        }
    }
}
