using BeekmanLabs.UnitTesting;
using IntegrationEngine.Model;
using Newtonsoft.Json;
using NUnit.Framework;
using Moq;
using RestSharp;
using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;

namespace IntegrationEngine.Client.Tests
{
    public class InEngineClientTest : TestBase<InEngineClient>
    {
        Mock<IRestClient> MockRestClient { get; set; }
        Mock<IJsonConvert> MockJsonConvert { get; set; }
        string ResourceStubName { get; set; }
        string HealthStatusName { get; set; }

        [SetUp]
        public void Setup()
        {
            MockRestClient = new Mock<IRestClient>();
            Subject.RestClient = MockRestClient.Object;
            MockJsonConvert = new Mock<IJsonConvert>();
            Subject.JsonConvert = MockJsonConvert.Object;
            ResourceStubName = typeof(ResourceStub).Name;
            HealthStatusName = typeof(HealthStatus).Name;
        }

        [Test]
        public void ShouldInstantiateWithDefaultApiUrl()
        {
            var expectedApiUrl = "http://localhost:9001/api/";
            var expected = new Uri(expectedApiUrl);
            MockRestClient.SetupGet(x => x.BaseUrl).Returns(expected);

            var actual = Subject.ApiUrl;

            Assert.That(actual.AbsoluteUri, Is.EqualTo(expected));
        }

        [Test]
        public void ShouldQueryTheHealthStatusEndpointAndReturnStatusCodeWhenPinging()
        {
            var restResponse = new RestResponse() { StatusCode = HttpStatusCode.OK };
            MockRestClient.Setup(x => x.Execute(It.Is<RestRequest>(y => y.Resource == HealthStatusName))).Returns(restResponse);

            var actual = Subject.Ping();

            MockRestClient.Verify(x => x.Execute(It.Is<RestRequest>(y => y.Resource == HealthStatusName)), Times.Once);
            Assert.That(actual, Is.TypeOf(typeof(HttpStatusCode)));
        }

        [Test]
        public void ShouldReturnHealthStatus()
        {
            var expected = new HealthStatus() {
                IsElasticsearchServerAvailable = true,
                IsMailServerAvailable = true,
                IsMessageQueueServerAvailable = true,
            };
            var content = JsonConvert.SerializeObject(expected);
            var restResponse = new RestResponse() { Content = content };
            MockRestClient.Setup(x => x.Execute(It.Is<RestRequest>(y => y.Resource == HealthStatusName)))
                .Returns(restResponse);
            MockJsonConvert.Setup(x => x.DeserializeObject<HealthStatus>(content))
                .Returns(expected);

            var actual = Subject.GetHealthStatus();

            Assert.That(actual, Is.EqualTo(expected));
            MockRestClient.Verify(x => x.Execute(It.Is<RestRequest>(y => y.Resource == HealthStatusName)), Times.Once);
            MockJsonConvert.Verify(x => x.DeserializeObject<HealthStatus>(content), Times.Once);
        }


        [Test]
        public void ShouldGetCollectionOfResourceStubs()
        {
            var expected = new List<ResourceStub>();
            var content = JsonConvert.SerializeObject(expected);
            var restResponse = new RestResponse() { Content = content };
            MockRestClient.Setup(x => x.Execute(It.Is<RestRequest>(y => y.Resource == ResourceStubName)))
                .Returns(restResponse);
            MockJsonConvert.Setup(x => x.DeserializeObject<IList<ResourceStub>>(content))
                .Returns(expected);

            var actual = Subject.GetCollection<ResourceStub>();

            Assert.That(actual.ToList(), Is.EquivalentTo(expected));
            MockRestClient.Verify(x => x.Execute(It.Is<RestRequest>(y => y.Resource == ResourceStubName)), Times.Once);
            MockJsonConvert.Verify(x => x.DeserializeObject<IList<ResourceStub>>(content), Times.Once);
        }

        [Test]
        public void ShouldReturnResourceStubGivenAnId()
        {
            var id = "1";
            var resource = ResourceStubName + "/{id}";
            var expected = new ResourceStub();
            var content = JsonConvert.SerializeObject(expected);
            var restResponse = new RestResponse() { Content = content };
            MockRestClient.Setup(x => x.Execute(It.Is<RestRequest>(y => y.Resource == resource)))
                .Returns(restResponse);
            MockJsonConvert.Setup(x => x.DeserializeObject<ResourceStub>(content))
                .Returns(expected);

            var actual = Subject.Get<ResourceStub>(id);

            Assert.That(actual, Is.EqualTo(expected));
            MockRestClient.Verify(x => x.Execute(It.Is<RestRequest>(y => y.Resource == resource)), Times.Once);
            MockJsonConvert.Verify(x => x.DeserializeObject<ResourceStub>(content), Times.Once);
        }

        [Test]
        public void ShouldCreateAndReturnResourceStub()
        {
            var expected = new ResourceStub();
            var content = JsonConvert.SerializeObject(expected);
            var restResponse = new RestResponse() { Content = content };
            MockRestClient.Setup(x => x.Execute(It.Is<RestRequest>(y => y.Resource == ResourceStubName)))
                .Returns(restResponse);
            MockJsonConvert.Setup(x => x.DeserializeObject<ResourceStub>(content))
                .Returns(expected);

            var actual = Subject.Create(expected);

            Assert.That(actual, Is.EqualTo(expected));
            MockRestClient.Verify(x => x.Execute(It.Is<RestRequest>(y => y.Resource == ResourceStubName)), Times.Once);
            MockJsonConvert.Verify(x => x.DeserializeObject<ResourceStub>(content), Times.Once);
        }

        [Test]
        public void ShouldUpdateAndReturnResourceStub()
        {
            var resource = ResourceStubName + "/{id}";
            var expected = new ResourceStub();
            var content = JsonConvert.SerializeObject(expected);
            var restResponse = new RestResponse() { Content = content };
            MockRestClient.Setup(x => x.Execute(It.Is<RestRequest>(y => y.Resource == resource)))
                .Returns(restResponse);
            MockJsonConvert.Setup(x => x.DeserializeObject<ResourceStub>(content))
                .Returns(expected);

            var actual = Subject.Update(expected);

            Assert.That(actual, Is.EqualTo(expected));
            MockRestClient.Verify(x => x.Execute(It.Is<RestRequest>(y => y.Resource == resource)), Times.Once);
            MockJsonConvert.Verify(x => x.DeserializeObject<ResourceStub>(content), Times.Once);
        }

        [Test]
        public void ShouldDeleteAndReturnResourceStub()
        {
            var id = "1";
            var resource = ResourceStubName + "/{id}";
            var expected = new ResourceStub();
            var content = JsonConvert.SerializeObject(expected);
            var restResponse = new RestResponse() { Content = content };
            MockRestClient.Setup(x => x.Execute(It.Is<RestRequest>(y => y.Resource == resource)))
                .Returns(restResponse);
            MockJsonConvert.Setup(x => x.DeserializeObject<ResourceStub>(content))
                .Returns(expected);

            var actual = Subject.Delete<ResourceStub>(id);

            Assert.That(actual, Is.EqualTo(expected));
            MockRestClient.Verify(x => x.Execute(It.Is<RestRequest>(y => y.Resource == resource)), Times.Once);
            MockJsonConvert.Verify(x => x.DeserializeObject<ResourceStub>(content), Times.Once);
        }
    }
}
