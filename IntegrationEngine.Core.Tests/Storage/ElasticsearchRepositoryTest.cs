using BeekmanLabs.UnitTesting;
using Common.Logging;
using Elasticsearch.Net;
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
        Mock<IElasticClient> ElasticClient { get; set; }
        Mock<ILog> Log { get; set; }

        [SetUp]
        public void Setup()
        {
            ElasticClient = new Mock<IElasticClient>();
            Subject.ElasticClient = ElasticClient.Object;
            Log = new Mock<ILog>();
            Subject.Log = Log.Object;
        }

        [Test]
        public void ShouldReturnListOfDocumentsWithIdsFromElasticsearch()
        {
            var searchResponse = new Mock<ISearchResponse<CronTrigger>>();
            var hits = new List<IHit<CronTrigger>>();
            var cronTrigger = new CronTrigger();
            var hit = new Mock<IHit<CronTrigger>>();
            var expectedId = "1";
            hit.SetupGet(x => x.Source).Returns(() => cronTrigger);
            hit.SetupGet(x => x.Id).Returns(() => expectedId);
            hits.Add(hit.Object);
            searchResponse.SetupGet(x => x.Hits).Returns(() => hits);
            ElasticClient.Setup(x => x.Search<CronTrigger>(It.IsAny<Func<SearchDescriptor<CronTrigger>, SearchDescriptor<CronTrigger>>>()))
                .Returns(searchResponse.Object);

            var actual = Subject.SelectAll<CronTrigger>();

            Assert.That(actual, Is.Not.Empty);
            Assert.That(actual.First().Id, Is.EqualTo(expectedId));
            ElasticClient.Verify(x => x.Search(It.IsAny<Func<SearchDescriptor<CronTrigger>, SearchDescriptor<CronTrigger>>>()), Times.Once);
        }

        [Test]
        public void ShouldReturnNullIfDocumentIsNotFoundById()
        {
            var getResponse = new Mock<IGetResponse<CronTrigger>>();
            ElasticClient.Setup(x => x.Get(It.IsAny<Func<GetDescriptor<CronTrigger>, GetDescriptor<CronTrigger>>>()))
                .Returns(getResponse.Object);

            var actual = Subject.SelectById<CronTrigger>("1");

            Assert.That(actual, Is.Null);
            ElasticClient.Verify(x => x.Get(It.IsAny<Func<GetDescriptor<CronTrigger>, GetDescriptor<CronTrigger>>>()), Times.Once);
        }

        void  SetupForGetDocument(string id) 
        {
            var getResponse = new Mock<IGetResponse<CronTrigger>>();
            getResponse.SetupGet(x => x.Id).Returns(() => id);
            getResponse.SetupGet(x => x.Source).Returns(() => new CronTrigger());
            ElasticClient.Setup(x => x.Get(It.IsAny<Func<GetDescriptor<CronTrigger>, GetDescriptor<CronTrigger>>>()))
                .Returns(getResponse.Object);
        }

        [Test]
        public void ShouldReturnSingleDocumentGivenAnId()
        {
            var expectedId = "1";
            SetupForGetDocument(expectedId);

            var actual = Subject.SelectById<CronTrigger>(expectedId);

            Assert.That(actual.Id, Is.EqualTo(expectedId));
            ElasticClient.Verify(x => x.Get(It.IsAny<Func<GetDescriptor<CronTrigger>, GetDescriptor<CronTrigger>>>()), Times.Once);
        }

        [Test]
        public void ShouldInsertAndReturnDocument()
        {
            var expectedId = "1";
            SetupForGetDocument(expectedId);
            var expected = new CronTrigger() { Id = expectedId };
            var indexResponse = new Mock<IIndexResponse>();
            indexResponse.SetupGet(x => x.Id).Returns(() => expected.Id);
            ElasticClient.Setup(x => x.Index(expected, It.IsAny<Func<IndexDescriptor<CronTrigger>, IndexDescriptor<CronTrigger>>>()))
                .Returns(indexResponse.Object);

            var actual = Subject.Insert(expected);

            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            ElasticClient.Verify(x => x.Index(expected, It.IsAny<Func<IndexDescriptor<CronTrigger>, IndexDescriptor<CronTrigger>>>()), Times.Once);
        }

        [Test]
        public void ShouldUpdateAndReturnDocument()
        {
            var expectedId = "1";
            SetupForGetDocument(expectedId);
            var expected = new CronTrigger() { Id = expectedId };
            var updateResponse = new Mock<IUpdateResponse>();
            updateResponse.SetupGet(x => x.Id).Returns(() => expected.Id);
            ElasticClient.Setup(x => x.Update<CronTrigger>(It.IsAny<Func<UpdateDescriptor<CronTrigger, CronTrigger>, UpdateDescriptor<CronTrigger, CronTrigger>>>()))
                .Returns(updateResponse.Object);

            var actual = Subject.Update(expected);

            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            ElasticClient.Verify(
                x => x.Update<CronTrigger>(It.IsAny<Func<UpdateDescriptor<CronTrigger, CronTrigger>, UpdateDescriptor<CronTrigger, CronTrigger>>>()), 
                Times.Once);
        }

        [Test]
        public void ShouldDeleteDocument()
        {
            var id = "1";
            ElasticClient.Setup(x => x.Delete(It.IsAny<Func<DeleteDescriptor<CronTrigger>, DeleteDescriptor<CronTrigger>>>()));

            Subject.Delete<CronTrigger>(id);

            ElasticClient.Verify(x => x.Delete(It.IsAny<Func<DeleteDescriptor<CronTrigger>, DeleteDescriptor<CronTrigger>>>()), Times.Once);
        }

        void SetupForDocumentExists(bool exists) 
        {
            var existsResponse = new Mock<IExistsResponse>();
            existsResponse.SetupGet(x => x.Exists).Returns(exists);
            ElasticClient.Setup(x => x.DocumentExists(It.IsAny<Func<DocumentExistsDescriptor<CronTrigger>, DocumentExistsDescriptor<CronTrigger>>>()))
                .Returns(existsResponse.Object);
        }

        [Test]
        public void ShouldReturnTrueIfDocumentExists()
        {
            SetupForDocumentExists(true);

            var actual = Subject.Exists<CronTrigger>("does not exist");

            Assert.That(actual, Is.True);
        }

        [Test]
        public void ShouldReturnFalseIfDocumentDoesNotExist()
        {
            SetupForDocumentExists(false);

            var actual = Subject.Exists<CronTrigger>("does not exist");

            Assert.That(actual, Is.False);
        }

        void SetupForIsServerAvailable(bool success) 
        {
            var elasticsearchResponse = new Mock<IElasticsearchResponse>();
            elasticsearchResponse.SetupGet(x => x.Success).Returns(() => success);
            var pingResponse = new Mock<IPingResponse>();
            pingResponse.SetupGet(x => x.ConnectionStatus).Returns(() => elasticsearchResponse.Object);
            ElasticClient.Setup(x => x.Ping(It.IsAny<PingRequest>())).Returns(pingResponse.Object);
        }

        [Test]
        public void ShouldShouldReturnTrueIfServerIsAvailable()
        {
            SetupForIsServerAvailable(true);

            var actual = Subject.IsServerAvailable();

            Assert.That(actual, Is.True);
        }

        [Test]
        public void ShouldShouldReturnFalseIfServerIsUnavailable()
        {
            SetupForIsServerAvailable(false);

            var actual = Subject.IsServerAvailable();

            Assert.That(actual, Is.False);
        }

        [Test]
        public void ShouldShouldReturnFalseIfServerIsUnavailableBecauseExceptionOccured()
        {
            ElasticClient.Setup(x => x.Ping(It.IsAny<PingRequest>())).Returns<PingResponse>(null);
            Log.Setup(x => x.Error(It.IsAny<Exception>()));

            var actual = Subject.IsServerAvailable();

            Assert.That(actual, Is.False);
            Log.Verify(x => x.Error(It.IsAny<Exception>()), Times.Once);
        }
    }
}
