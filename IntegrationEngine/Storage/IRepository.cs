using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationEngine.Storage
{
    interface IRepository<T> where T : class
    {
        IEnumerable<T> SelectAll();
        T SelectById(object id);
        void Insert(T value);
        void Update(T value);
        void Delete(object id);
        void Save();
        bool Exists(object id);
    }
}
