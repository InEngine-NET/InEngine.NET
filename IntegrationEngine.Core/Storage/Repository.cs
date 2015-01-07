using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.Storage
{
    public class Repository<T> : IRepository<T> where T : class
    {
        public IntegrationEngineContext db = null;
        public DbSet<T> table = null;

        public Repository()
        {
        }

        public Repository(IntegrationEngineContext db)
        {
            this.db = db;
            table = db.Set<T>();
        }

        public IEnumerable<T> SelectAll()
        {
            return table.ToList();
        }

        public T SelectById(object id)
        {
            return table.Find(id);
        }

        public T Insert(T value)
        {
            return table.Add(value);
        }

        public T Update(T value)
        {
            table.Attach(value);
            db.Entry(value).State = EntityState.Modified;
            return db.Entry(value).Entity;
        }

        public void Delete(object id)
        {
            T existing = table.Find(id);
            table.Remove(existing);
        }

        public void Save()
        {
            db.SaveChanges();
        }

        public bool Exists(object id)
        {
            return table.Find(id) != null;
        }

        public void SetState(T value, EntityState entityState)
        {
            db.Entry(value).State = entityState;
        }

        public void Dispose()
        {
            db.Dispose();
        }
    }
}
