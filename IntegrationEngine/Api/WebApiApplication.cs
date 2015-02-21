using IntegrationEngine.Core.Configuration;
using Microsoft.Owin.Hosting;
using System;
using System.Web.Http;
using Microsoft.Practices.Unity;
using System.Linq;
using System.Web.Http.Cors;
using Owin;

namespace IntegrationEngine.Api
{
    public class WebApiApplication : IWebApiApplication
    {
        public IDisposable webApi { get; set; }
        public WebApiConfiguration WebApiConfiguration { get; set; }
        public ContainerResolver ContainerResolver { get; set; }

        public WebApiApplication()
        {}

        public void Start()
        {
            var baseAddress = (new UriBuilder("http", WebApiConfiguration.HostName, WebApiConfiguration.Port)).Uri.AbsoluteUri;
            Action<Owin.IAppBuilder> startup = x => {
                var  config = new HttpConfiguration();
                config.DependencyResolver = ContainerResolver;
//                if (WebApiConfiguration != null && WebApiConfiguration.Origins.Any())
//                    config.EnableCors(new EnableCorsAttribute(string.Join(",", WebApiConfiguration.Origins), "*", "*"));
                config.Routes.MapHttpRoute(
                    name: "DefaultApi",
                    routeTemplate: "api/{controller}/{id}",
                    defaults: new { id = RouteParameter.Optional }
                );
                config.MapHttpAttributeRoutes();
                x.UseWebApi(config);
            };
            webApi = WebApp.Start(baseAddress, startup);
        }

        public void Dispose()
        {
            if (webApi != null)
                webApi.Dispose();
        }
    }
}
