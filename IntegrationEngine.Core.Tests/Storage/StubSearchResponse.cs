using Elasticsearch.Net;
using Nest;
using Nest.Domain;
using System;
using System.Collections.Generic;

namespace IntegrationEngine.Core.Tests.Storage
{
    public class StubSearchResponse<TDocument> : ISearchResponse<TDocument> where TDocument : class
    {
        public StubSearchResponse()
        {
        }

        #region ISearchResponse implementation

        public F Facet<F>(System.Linq.Expressions.Expression<Func<TDocument, object>> expression) where F : class, IFacet
        {
            throw new NotImplementedException();
        }

        public F Facet<F>(string fieldName) where F : class, IFacet
        {
            throw new NotImplementedException();
        }

        public IEnumerable<F> FacetItems<F>(System.Linq.Expressions.Expression<Func<TDocument, object>> expression) where F : FacetItem
        {
            throw new NotImplementedException();
        }

        public IEnumerable<F> FacetItems<F>(string fieldName) where F : FacetItem
        {
            throw new NotImplementedException();
        }

        public ShardsMetaData Shards {
            get {
                throw new NotImplementedException();
            }
        }

        public HitsMetaData<TDocument> HitsMetaData {
            get {
                throw new NotImplementedException();
            }
        }

        public IDictionary<string, Facet> Facets {
            get {
                throw new NotImplementedException();
            }
        }

        public IDictionary<string, IAggregation> Aggregations {
            get {
                throw new NotImplementedException();
            }
        }

        public AggregationsHelper Aggs {
            get {
                throw new NotImplementedException();
            }
        }

        public IDictionary<string, Suggest[]> Suggest {
            get {
                throw new NotImplementedException();
            }
        }

        public int ElapsedMilliseconds {
            get {
                throw new NotImplementedException();
            }
        }

        public bool TimedOut {
            get {
                throw new NotImplementedException();
            }
        }

        public bool TerminatedEarly {
            get {
                throw new NotImplementedException();
            }
        }

        public string ScrollId {
            get {
                throw new NotImplementedException();
            }
        }

        public long Total {
            get {
                throw new NotImplementedException();
            }
        }

        public double MaxScore {
            get {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<TDocument> Documents {
            get {
                throw new NotImplementedException();
            }
        }

        public virtual IEnumerable<IHit<TDocument>> Hits {
            get {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<FieldSelection<TDocument>> FieldSelections {
            get {
                throw new NotImplementedException();
            }
        }

        public HighlightDocumentDictionary Highlights {
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
