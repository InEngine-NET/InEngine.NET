using System;
using Nest;

namespace IntegrationEngine.Core.Tests.Storage
{
    public class StubGetResponse<TItem> : IGetResponse<TItem> where TItem : class
    {
        public StubGetResponse()
        {
        }

        #region IGetResponse implementation

        public bool Found {
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

        public virtual TItem Source {
            get {
                throw new NotImplementedException();
            }
        }

        public Nest.Domain.FieldSelection<TItem> Fields {
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

