using BeekmanLabs.UnitTesting;
using IntegrationEngine.Core.Storage;
using IntegrationEngine.Model;
using Moq;
using Nest;
using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;

namespace IntegrationEngine.Core.Tests.Storage
{
    public class ElasticsearchRepositoryTest : TestBase<ElasticsearchRepository>
    {
        [Test]
        public void ShouldReturnListOfDocumentsWithIdsFromElasticsearch()
        {
            var elasticClient = new Mock<StubElasticClient>();
            var searchResponse = new Mock<StubSearchResponse<CronTrigger>>();
            var hits = new List<IHit<CronTrigger>>();
            var cronTrigger = new CronTrigger();
            var hit = new Mock<StubHit<CronTrigger>>();
            var expectedId = "1";
            hit.SetupGet(x => x.Source).Returns(() => cronTrigger);
            hit.SetupGet(x => x.Id).Returns(() => expectedId);
            hits.Add(hit.Object);
            searchResponse.SetupGet(x => x.Hits).Returns(() => hits);
            elasticClient.Setup(x => x.Search<CronTrigger>(It.IsAny<Func<SearchDescriptor<CronTrigger>, SearchDescriptor<CronTrigger>>>()))
                .Returns(searchResponse.Object);
            Subject.ElasticClient = elasticClient.Object;

            var actual = Subject.SelectAll<CronTrigger>();

            Assert.That(actual, Is.Not.Empty);
            Assert.That(actual.First().Id, Is.EqualTo(expectedId));
        }
    }
}
