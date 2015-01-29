using System;
using System.Data.Entity;

namespace IntegrationEngine.Core.Storage
{
    public interface IDatabaseRepository : IRepository, IDisposable
    {
        void Save();
        void SetState<TItem>(TItem item, EntityState entityState) where TItem : class;
    }
}

