using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.Storage
{
    public interface IRepository<TId>
    {
        IEnumerable<TItem> SelectAll<TItem>() where TItem : class, TId;
        TItem SelectById<TItem>(object id) where TItem : class, TId;
        TItem Insert<TItem>(TItem item) where TItem : class, TId;
        TItem Update<TItem>(TItem item) where TItem : class, TId;
        void Delete<TItem>(object id) where TItem : class;
        bool Exists<TItem>(object id) where TItem : class;
        bool IsServerAvailable();
    }
}
