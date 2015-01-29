using IntegrationEngine.Model;
using System;
using System.Collections.Generic;

namespace IntegrationEngine.Core.Storage
{
    public interface IElasticsearchRepository : IRepository
    {
        new IEnumerable<TItem> SelectAll<TItem>() where TItem : class, IHasStringId;
        new TItem SelectById<TItem>(object id) where TItem : class, IHasStringId;
        new TItem Update<TItem>(TItem item) where TItem : class, IHasStringId;
        new TItem Insert<TItem>(TItem item) where TItem : class, IHasStringId;
    }
}

