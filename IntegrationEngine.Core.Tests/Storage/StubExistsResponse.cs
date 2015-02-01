using Nest;
using System;

namespace IntegrationEngine.Core.Tests.Storage
{
    public class StubExistsResponse : IExistsResponse
    {
        public StubExistsResponse()
        {
        }

        #region IExistsResponse implementation

        public virtual bool Exists {
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

        public Elasticsearch.Net.IElasticsearchResponse ConnectionStatus {
            get {
                throw new NotImplementedException();
            }
        }

        public ElasticInferrer Infer {
            get {
                throw new NotImplementedException();
            }
        }

        public Elasticsearch.Net.ElasticsearchServerError ServerError {
            get {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region IResponseWithRequestInformation implementation

        public Elasticsearch.Net.IElasticsearchResponse RequestInformation {
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

