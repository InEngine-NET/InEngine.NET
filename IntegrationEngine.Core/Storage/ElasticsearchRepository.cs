using Common.Logging;
using IntegrationEngine.Model;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IntegrationEngine.Core.Storage
{
    public class ElasticsearchRepository : IElasticsearchRepository
    {
        public IElasticClient ElasticClient { get; set; }
        public ILog Log { get; set; }

        public ElasticsearchRepository()
        {
        }

        public IEnumerable<TItem> SelectAll<TItem>() where TItem : class, IHasStringId
        {
            var response = ElasticClient.Search<TItem>(x => x);
            var hits = response.Hits;
            var documents = hits.Select(h => {
                    h.Source.Id = h.Id;
                    return h.Source;
            });
            var list = documents.ToList();
            return list;
        }

        public TItem SelectById<TItem>(object id) where TItem : class, IHasStringId
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
            return SelectById<TItem>(ElasticClient.Index<TItem>(item).Id);
        }

        public TItem Update<TItem>(TItem item) where TItem : class, IHasStringId
        {
            var response = ElasticClient.Update<TItem, object>(x => x
                .Id(item.Id)
                .Doc(item)
            );
            return response as TItem;
        }
            
        public void Delete<TItem>(object id) where TItem : class
        {
            ElasticClient.Delete<TItem>(x => x.Id(id.ToString()));
        }

        public bool Exists<TItem>(object id) where TItem : class
        {
            return ElasticClient.DocumentExists<TItem>(x => x.Id(id.ToString())).Exists;
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

