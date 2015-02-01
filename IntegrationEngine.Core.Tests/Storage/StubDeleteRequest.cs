using Elasticsearch.Net;
using Elasticsearch.Net.Connection.Configuration;
using Nest;
using System;

namespace IntegrationEngine.Core.Tests.Storage
{
    public class StubDeleteRequest : IDeleteRequest
    {
        public StubDeleteRequest()
        {
        }

        #region IPathInfo implementation

        public ElasticsearchPathInfo<DeleteRequestParameters> ToPathInfo(IConnectionSettingsValues settings)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IDocumentOptionalPath implementation

        public IndexNameMarker Index {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public TypeNameMarker Type {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        public string Id {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region IRequest implementation

        public Elasticsearch.Net.DeleteRequestParameters RequestParameters {
            get {
                throw new NotImplementedException();
            }
            set {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region IRequest implementation

        public IRequestConfiguration RequestConfiguration {
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

