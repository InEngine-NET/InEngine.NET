using Elasticsearch.Net;
using Nest;
using System;

namespace IntegrationEngine.Core.Tests.Storage
{
    public class StubIndexResponse : IIndexResponse
    {
        public StubIndexResponse()
        {
        }

        #region IIndexResponse implementation

        public virtual string Id {
            get {
                throw new NotImplementedException();
            }
        }

        public string Index {
            get {
                throw new NotImplementedException();
            }
        }

        public string Type {
            get {
                throw new NotImplementedException();
            }
        }

        public string Version {
            get {
                throw new NotImplementedException();
            }
        }

        public bool Created {
            get {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region IResponse implementation

        public bool IsValid {
            get {
                throw new NotImplementedException();
            }
        }

        public IElasticsearchResponse ConnectionStatus {
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

