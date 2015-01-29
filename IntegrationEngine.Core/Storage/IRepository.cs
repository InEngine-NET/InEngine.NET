using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.Storage
{
    public interface IRepository
    {
        IEnumerable<TItem> SelectAll<TItem>() where TItem : class;
        TItem SelectById<TItem>(object id) where TItem : class;
        TItem Insert<TItem>(TItem item) where TItem : class;
        TItem Update<TItem>(TItem item) where TItem : class;
        void Delete<TItem>(object id) where TItem : class;
        bool Exists<TItem>(object id) where TItem : class;
        bool IsServerAvailable();
    }
}
