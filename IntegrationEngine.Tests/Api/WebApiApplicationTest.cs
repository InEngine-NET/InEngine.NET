using BeekmanLabs.UnitTesting;
using IntegrationEngine.Api;
using IntegrationEngine.Core.Configuration;
using Microsoft.Owin.Hosting;
using Microsoft.Practices.Unity;
using NUnit.Framework;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using Moq;
using System.Web.Http.Routing;

namespace IntegrationEngine.Tests.Api
{
    public class WebApiApplicationTest : TestBase<WebApiApplication>
    {

        [Test]
        public void ShouldSetDependencyResolver()
        {
            var expected = new ContainerResolver();
            Subject.ContainerResolver = expected;

            var httpConfiguration = Subject.HttpConfigurationFactory();

            Assert.That(httpConfiguration.DependencyResolver, Is.SameAs(expected));
        }

        [Test]
        public void ShouldConfigureCors()
        {
            var webApiConfiguration = new WebApiConfiguration() {
                Origins = new List<string> { "*" },
            };
            Subject.WebApiConfiguration = webApiConfiguration;
            Subject.ContainerResolver = new ContainerResolver();

            var httpConfiguration = Subject.HttpConfigurationFactory();

            var policyProviderFactory = (httpConfiguration.GetCorsPolicyProviderFactory() as AttributeBasedPolicyProviderFactory);
            Assert.That(policyProviderFactory.DefaultPolicyProvider, Is.Not.Null);
        }

        [Test]
        public void ShouldConfigureRoutes()
        {
            var expected = new ContainerResolver();
            Subject.ContainerResolver = expected;

            var httpConfiguration = Subject.HttpConfigurationFactory();

            Assert.That(httpConfiguration.Routes.Count, Is.EqualTo(2));
            IHttpRoute defaultHttpRoute;
            httpConfiguration.Routes.TryGetValue("DefaultApi", out defaultHttpRoute);
            Assert.That(defaultHttpRoute.RouteTemplate, Is.EqualTo("api/{controller}/{id}"));
            Assert.That(defaultHttpRoute.Defaults.Keys.Contains("id"), Is.True);
            Assert.That(httpConfiguration.Routes.ContainsKey("MS_attributerouteWebApi"), Is.True);
        }
    }
}
