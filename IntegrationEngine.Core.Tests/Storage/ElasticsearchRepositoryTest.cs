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
            var searchResponse = new Mock<ISearchResponse<DocumentStub>>();
            var hits = new List<IHit<DocumentStub>>();
            var cronTrigger = new DocumentStub();
            var hit = new Mock<IHit<DocumentStub>>();
            var expectedId = "1";
            hit.SetupGet(x => x.Source).Returns(() => cronTrigger);
            hit.SetupGet(x => x.Id).Returns(() => expectedId);
            hits.Add(hit.Object);
            searchResponse.SetupGet(x => x.Hits).Returns(() => hits);
            ElasticClient.Setup(x => x.Search<DocumentStub>(It.IsAny<Func<SearchDescriptor<DocumentStub>, SearchDescriptor<DocumentStub>>>()))
                .Returns(searchResponse.Object);

            var actual = Subject.SelectAll<DocumentStub>();

            Assert.That(actual, Is.Not.Empty);
            Assert.That(actual.First().Id, Is.EqualTo(expectedId));
            ElasticClient.Verify(x => x.Search(It.IsAny<Func<SearchDescriptor<DocumentStub>, SearchDescriptor<DocumentStub>>>()), Times.Once);
        }

        [Test]
        public void ShouldReturnNullIfDocumentIsNotFoundById()
        {
            var getResponse = new Mock<IGetResponse<DocumentStub>>();
            ElasticClient.Setup(x => x.Get(It.IsAny<Func<GetDescriptor<DocumentStub>, GetDescriptor<DocumentStub>>>()))
                .Returns(getResponse.Object);

            var actual = Subject.SelectById<DocumentStub>("1");

            Assert.That(actual, Is.Null);
            ElasticClient.Verify(x => x.Get(It.IsAny<Func<GetDescriptor<DocumentStub>, GetDescriptor<DocumentStub>>>()), Times.Once);
        }

        void  SetupForGetDocument(string id) 
        {
            var getResponse = new Mock<IGetResponse<DocumentStub>>();
            getResponse.SetupGet(x => x.Id).Returns(() => id);
            getResponse.SetupGet(x => x.Source).Returns(() => new DocumentStub());
            ElasticClient.Setup(x => x.Get(It.IsAny<Func<GetDescriptor<DocumentStub>, GetDescriptor<DocumentStub>>>()))
                .Returns(getResponse.Object);
        }

        [Test]
        public void ShouldReturnSingleDocumentGivenAnId()
        {
            var expectedId = "1";
            SetupForGetDocument(expectedId);

            var actual = Subject.SelectById<DocumentStub>(expectedId);

            Assert.That(actual.Id, Is.EqualTo(expectedId));
            ElasticClient.Verify(x => x.Get(It.IsAny<Func<GetDescriptor<DocumentStub>, GetDescriptor<DocumentStub>>>()), Times.Once);
        }

        [Test]
        public void ShouldInsertAndReturnDocument()
        {
            var expectedId = "1";
            SetupForGetDocument(expectedId);
            var expected = new DocumentStub() { Id = expectedId };
            var indexResponse = new Mock<IIndexResponse>();
            indexResponse.SetupGet(x => x.Id).Returns(() => expected.Id);
            ElasticClient.Setup(x => x.Index(expected, It.IsAny<Func<IndexDescriptor<DocumentStub>, IndexDescriptor<DocumentStub>>>()))
                .Returns(indexResponse.Object);

            var actual = Subject.Insert(expected);

            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            ElasticClient.Verify(x => x.Index(expected, It.IsAny<Func<IndexDescriptor<DocumentStub>, IndexDescriptor<DocumentStub>>>()), Times.Once);
        }

        [Test]
        public void ShouldUpdateAndReturnDocument()
        {
            var expectedId = "1";
            SetupForGetDocument(expectedId);
            var expected = new DocumentStub() { Id = expectedId };
            var updateResponse = new Mock<IUpdateResponse>();
            updateResponse.SetupGet(x => x.Id).Returns(() => expected.Id);
            ElasticClient.Setup(x => x.Update<DocumentStub>(It.IsAny<Func<UpdateDescriptor<DocumentStub, DocumentStub>, UpdateDescriptor<DocumentStub, DocumentStub>>>()))
                .Returns(updateResponse.Object);

            var actual = Subject.Update(expected);

            Assert.That(actual.Id, Is.EqualTo(expected.Id));
            ElasticClient.Verify(
                x => x.Update<DocumentStub>(It.IsAny<Func<UpdateDescriptor<DocumentStub, DocumentStub>, UpdateDescriptor<DocumentStub, DocumentStub>>>()), 
                Times.Once);
        }

        [Test]
        public void ShouldDeleteDocument()
        {
            var id = "1";
            ElasticClient.Setup(x => x.Delete(It.IsAny<Func<DeleteDescriptor<DocumentStub>, DeleteDescriptor<DocumentStub>>>()));

            Subject.Delete<DocumentStub>(id);

            ElasticClient.Verify(x => x.Delete(It.IsAny<Func<DeleteDescriptor<DocumentStub>, DeleteDescriptor<DocumentStub>>>()), Times.Once);
        }

        void SetupForDocumentExists(bool exists) 
        {
            var existsResponse = new Mock<IExistsResponse>();
            existsResponse.SetupGet(x => x.Exists).Returns(exists);
            ElasticClient.Setup(x => x.DocumentExists(It.IsAny<Func<DocumentExistsDescriptor<DocumentStub>, DocumentExistsDescriptor<DocumentStub>>>()))
                .Returns(existsResponse.Object);
        }

        [Test]
        public void ShouldReturnTrueIfDocumentExists()
        {
            SetupForDocumentExists(true);

            var actual = Subject.Exists<DocumentStub>("does not exist");

            Assert.That(actual, Is.True);
        }

        [Test]
        public void ShouldReturnFalseIfDocumentDoesNotExist()
        {
            SetupForDocumentExists(false);

            var actual = Subject.Exists<DocumentStub>("does not exist");

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
