using IntegrationEngine.Model;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;

namespace IntegrationEngine.Core.Storage
{
    public interface IElasticsearchRepository : IRepository<IHasStringId>
    {
        IEnumerable<TItem> SelectAll<TItem, TKey>(Expression<Func<TItem, TKey>> orderBy, bool ascending = true, int pageIndex = 0, int rowCount = 10) where TItem : class, IHasStringId;
        IEnumerable<TItem> Search<TItem, TKey>(string query, Expression<Func<TItem, TKey>> orderBy, bool ascending = true, int pageIndex = 0, int rowCount = 10) where TItem : class, IHasStringId;
    }
}

