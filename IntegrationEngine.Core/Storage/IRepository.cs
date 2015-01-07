using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Core.Storage
{
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> SelectAll();
        T SelectById(object id);
        T Insert(T value);
        T Update(T value);
        void Delete(object id);
        void Save();
        bool Exists(object id);
        void SetState(T value, EntityState entityState);
        void Dispose();
    }
}
