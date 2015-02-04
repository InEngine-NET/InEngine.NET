using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.Storage
{
    public interface IRepository<THasId, TId>
    {
        IEnumerable<TItem> SelectAll<TItem>() where TItem : class, THasId;
        TItem SelectById<TItem>(TId id) where TItem : class, THasId;
        TItem Insert<TItem>(TItem item) where TItem : class, THasId;
        TItem Update<TItem>(TItem item) where TItem : class, THasId;
        void Delete<TItem>(TId id) where TItem : class;
        bool Exists<TItem>(TId id) where TItem : class;
        bool IsServerAvailable();
    }
}
