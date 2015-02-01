using Elasticsearch.Net;
using Elasticsearch.Net.Connection;
using System;

namespace IntegrationEngine.Core.Tests.Storage
{
    public class StubElasticsearchResponse : IElasticsearchResponse
    {
        public StubElasticsearchResponse()
        {
        }

        #region IElasticsearchResponse implementation

        public virtual bool Success {
            get {
                throw new NotImplementedException();
            }
        }

        public IConnectionConfigurationValues Settings {
            get {
                throw new NotImplementedException();
            }
        }

        public Exception OriginalException {
            get {
                throw new NotImplementedException();
            }
        }

        public string RequestMethod {
            get {
                throw new NotImplementedException();
            }
        }

        public string RequestUrl {
            get {
                throw new NotImplementedException();
            }
        }

        public int? HttpStatusCode {
            get {
                throw new NotImplementedException();
            }
        }

        public int NumberOfRetries {
            get {
                throw new NotImplementedException();
            }
        }

        public CallMetrics Metrics {
            get {
                throw new NotImplementedException();
            }
        }

        public byte[] ResponseRaw {
            get {
                throw new NotImplementedException();
            }
        }

        public byte[] Request {
            get {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}

