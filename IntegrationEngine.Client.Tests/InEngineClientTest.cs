using BeekmanLabs.UnitTesting;
using NUnit.Framework;
using Moq;
using RestSharp;
using System;
using System.Net;

namespace IntegrationEngine.Client.Tests
{
    public class InEngineClientTest : TestBase<InEngineClient>
    {
        [Test]
        public void ShouldInstantiateWithDefaultApiUrl()
        {
            var expected = "http://localhost:9001/api/";

            var actual = Subject.ApiUrl;

            Assert.That(actual.AbsoluteUri, Is.EqualTo(expected));
        }

        [Test]
        public void ShouldQueryTheHealthStatusEndpointAndReturnStatusCodeWhenPinging()
        {
            var mockRestClient = new Mock<IRestClient>();
            var restResponse = new RestResponse() { StatusCode = HttpStatusCode.OK};
            mockRestClient.Setup(x => x.Execute(It.Is<RestRequest>(y => y.Resource == EndpointName.HealthStatus)))
                .Returns(restResponse);
            Subject.RestClient = mockRestClient.Object;

            var actual = Subject.Ping();

            mockRestClient.Verify(x => x.Execute(It.Is<RestRequest>(y => y.Resource == EndpointName.HealthStatus)), Times.Once);
            Assert.That(actual, Is.TypeOf(typeof(HttpStatusCode)));
        }
    }
}
