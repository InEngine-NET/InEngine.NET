using Microsoft.Owin;
using Owin;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using Microsoft.Practices.Unity;
using IntegrationEngine.Configuration;

[assembly: OwinStartup(typeof(IntegrationEngine.Api.Startup))]

namespace IntegrationEngine.Api
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            var  config = new HttpConfiguration();
            var container = ContainerSingleton.GetContainer();
            config.DependencyResolver = new ContainerResolver(container);
            var webApiConfig = container.Resolve<EngineConfiguration>().WebApi;
            if (webApiConfig.Origins.Any())
                config.EnableCors(new EnableCorsAttribute(string.Join(",", webApiConfig.Origins), "*", "*"));
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            config.MapHttpAttributeRoutes();
            appBuilder.UseWebApi(config);
        }
    }
}
