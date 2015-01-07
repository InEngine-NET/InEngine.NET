using System;
using System.Linq;
using Nest;
using System.Collections.Generic;

namespace IntegrationEngine.Core.Storage
{
    public class ESRepository<T> : IRepository<T> where T : class, IHasStringId
    {
        public ElasticClient ElasticClient{ get; set; }

        public ESRepository()
        {
        }

        public IEnumerable<T> SelectAll()
        {
            var response = ElasticClient.Search<T>(x => x);
            var documents = response.Hits.Select(h => {
                h.Source.Id = h.Id;
                return h.Source;
            }).ToList();
            return documents;
        }

        public T SelectById(object id)
        {
            var response = ElasticClient.Get<T>(x => x.Id(id.ToString()));
            if (response.Source == null)
                return null;
            var item = response.Source;
            item.Id = response.Id;
            return item;
        }

        public T Insert(T value)
        {
            var document = SelectById(ElasticClient.Index<T>(value).Id);
            return document;
        }

        public T Update(T value)
        {
            var response = ElasticClient.Update<T, object>(x => x
                .Id(value.Id)
                .Doc(value)
            );
            return response as T;
        }

        public void Delete(object id)
        {
            ElasticClient.Delete<T>(x => x.Id(id.ToString()));
        }

        public void Save()
        {
            throw new NotImplementedException();
        }

        public bool Exists(object id)
        {
            return ElasticClient.DocumentExists<T>(x => x.Id(id.ToString())).Exists;
        }

        public void SetState(T value, System.Data.Entity.EntityState entityState)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

