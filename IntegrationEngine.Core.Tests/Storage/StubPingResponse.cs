using Elasticsearch.Net;
using Nest;
using System;

namespace IntegrationEngine.Core.Tests.Storage
{
    public class StubPingResponse : IPingResponse
    {
        public StubPingResponse()
        {
        }

        #region IResponse implementation

        public bool IsValid {
            get {
                throw new NotImplementedException();
            }
        }

        public virtual IElasticsearchResponse ConnectionStatus {
            get {
                throw new NotImplementedException();
            }
        }

        public ElasticInferrer Infer {
            get {
                throw new NotImplementedException();
            }
        }

        public ElasticsearchServerError ServerError {
            get {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region IResponseWithRequestInformation implementation

        public IElasticsearchResponse RequestInformation {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}

