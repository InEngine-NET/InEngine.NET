using Nest;
using System;

namespace IntegrationEngine.Core.Tests
{
    public class StubUpdateResponse : IUpdateResponse
    {
        public StubUpdateResponse()
        {
        }

        #region IUpdateResponse implementation

        public ShardsMetaData ShardsHit {
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

        public virtual string Id {
            get {
                throw new NotImplementedException();
            }
        }

        public string Version {
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

