using Common.Logging;using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IntegrationEngine.Model;

namespace IntegrationEngine.Core.Storage
{
    public class DatabaseRepository : IDatabaseRepository
    {
        public ILog Log { get; set; }
        public IntegrationEngineContext db = null;

        public DatabaseRepository()
        {
        }

        public DatabaseRepository(IntegrationEngineContext db)
        {
            this.db = db;
        }

        public IEnumerable<TItem> SelectAll<TItem>() where TItem : class, IHasLongId
        {
            return db.Set<TItem>().ToList<TItem>();
        }

        public TItem SelectById<TItem>(object id) where TItem : class, IHasLongId
        {
            return db.Set<TItem>().Find(id);

        }

        public TItem Insert<TItem>(TItem item) where TItem : class, IHasLongId
        {
            return db.Set<TItem>().Add(item);
        }

        public TItem Update<TItem>(TItem item) where TItem : class, IHasLongId
        {
            db.Set<TItem>().Attach(item);
            db.Entry(item).State = EntityState.Modified;
            return db.Entry(item).Entity;
        }

        public void Delete<TItem>(object id) where TItem : class
        {
            TItem existing = db.Set<TItem>().Find(id);
            db.Set<TItem>().Remove(existing);
        }

        public void Save()
        {
            db.SaveChanges();
        }

        public bool Exists<TItem>(object id) where TItem : class
        {
            return db.Set<TItem>().Find(id) != null;
        }

        public void SetState<TItem>(TItem item, EntityState entityState) where TItem : class
        {
            db.Entry(item).State = entityState;
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public bool IsServerAvailable()
        {
            try 
            {
                db.Database.Connection.Open();
                return true;
            }
            catch(Exception exception) 
            {
                Log.Error(exception);
                return false;
            }
        }
    }
}
