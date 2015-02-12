using Common.Logging;
using IntegrationEngine.Model;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IntegrationEngine.Core.Storage
{
    public class ElasticsearchRepository : IElasticsearchRepository
    {
        public IElasticClient ElasticClient { get; set; }
        public ILog Log { get; set; }

        public ElasticsearchRepository()
        {
            Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        }

        public IEnumerable<TItem> SelectAll<TItem>() where TItem : class, IHasStringId
        {
            var response = ElasticClient.Search<TItem>(x => x);
            var hits = response.Hits;
            var documents = hits.Select(h => {
                    h.Source.Id = h.Id;
                    return h.Source;
            });
            return documents.ToList();
        }

        public IEnumerable<TItem> SelectAll<TItem, TKey>(Expression<Func<TItem, TKey>> orderBy, 
            bool ascending = true, 
            int pageIndex = 0, 
            int rowCount = 10) 
            where TItem : class, IHasStringId
        {
            var documents = ElasticClient.Search<TItem>(x => x.From(pageIndex).Size(rowCount))
                .Hits
                .Select(h => {
                    h.Source.Id = h.Id;
                    return h.Source;
                }).AsQueryable();
            if (ascending)
                documents.OrderBy(orderBy);
            else 
                documents.OrderByDescending(orderBy);
            return documents.ToList();
        }

        public IEnumerable<TItem> Search<TItem, TKey>(string query, 
            Expression<Func<TItem, TKey>> orderBy,
            bool ascending = true,
            int pageIndex = 0,
            int rowCount = 10) 
            where TItem : class, IHasStringId
        {
            var documents = ElasticClient.Search<TItem>(x => x
                .From(pageIndex)
                .Size(rowCount)
                .Query(q => q.Match(m => m.OnField("_all").Query(query)) || q.Fuzzy(fd => fd.OnField("_all").PrefixLength(1).Value(query).Boost(0.1))))
                .Hits
                .Select(h => {
                    h.Source.Id = h.Id;
                    return h.Source;
                }).AsQueryable();
            if (ascending)
                documents.OrderBy(orderBy);
            else 
                documents.OrderByDescending(orderBy);
            return documents.AsEnumerable();
        }

        public TItem SelectById<TItem>(string id) where TItem : class, IHasStringId
        {
            var response = ElasticClient.Get<TItem>(x => x.Id(id.ToString()));
            if (response.Source == null)
                return null;
            var item = response.Source;
            item.Id = response.Id;
            return item;
        }

        public TItem Insert<TItem>(TItem item) where TItem : class, IHasStringId
        {
            var indexResponse = ElasticClient.Index<TItem>(item);
            return SelectById<TItem>(indexResponse.Id);
        }

        public TItem Update<TItem>(TItem item) where TItem : class, IHasStringId
        {
            var updateResponse = ElasticClient.Update<TItem>(x => x
                .Id(item.Id)
                .Doc(item)
            );
            return SelectById<TItem>(updateResponse.Id);
        }

        public void Delete<TItem>(string id) where TItem : class
        {
            ElasticClient.Delete<TItem>(x => x.Id(id.ToString()));
        }

        public bool Exists<TItem>(string id) where TItem : class
        {
            return ElasticClient.DocumentExists<TItem>(x => x.Id(id)).Exists;
        }

        public bool IsServerAvailable()
        {
            try 
            {
                return ElasticClient.Ping(new PingRequest()).ConnectionStatus.Success;
            }
            catch(Exception exception) 
            {
                Log.Error(exception);
                return false;
            }
        }
    }
}

