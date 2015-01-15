using Microsoft.Owin;
using Owin;
using System.Linq;
using System.Web.Http;
using Microsoft.Practices.Unity;

[assembly: OwinStartup(typeof(IntegrationEngine.Api.Startup))]

namespace IntegrationEngine.Api
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            var  config = new HttpConfiguration();
            config.DependencyResolver = new ContainerResolver(ContainerSingleton.GetContainer());
            config.EnableCors();
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
