using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Storage
{
    public interface IStorage
    {
        void Store<T>(string key, T value);
        void StoreOrUpdate<T>(string key, T value);
        void Update<T>(string key, T newValue);
        T Retrieve<T>(string key);
    }
}
