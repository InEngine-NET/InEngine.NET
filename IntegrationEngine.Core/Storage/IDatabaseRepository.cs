using IntegrationEngine.Model;
using System;
using System.Data.Entity;

namespace IntegrationEngine.Core.Storage
{
    public interface IDatabaseRepository : IRepository<IHasLongId>, IDisposable
    {
        void Save();
        void SetState<TItem>(TItem item, EntityState entityState) where TItem : class;
    }
}

