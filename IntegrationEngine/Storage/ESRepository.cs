using System;
using Nest;
using System.Collections.Generic;

namespace IntegrationEngine.Storage
{
    public class ESRepository<T> : IRepository<T> where T : class, IHasStringId
    {
        public ElasticClient ElasticClient{ get; set; }

        public ESRepository()
        {
        }

        public IEnumerable<T> SelectAll()
        {
            return ElasticClient.Search<T>(x => x).Documents;
        }

        public T SelectById(object id)
        {
            return ElasticClient.Get<T>(id.ToString()) as T;
        }

        public void Insert(T value)
        {
            ElasticClient.Index<T>(value);
        }

        public void Update(T value)
        {
            ElasticClient.Update<T, object>(x => x
                .Id(value.Id)
                .Doc(value)
            );
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

