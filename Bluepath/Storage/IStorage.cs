namespace Bluepath.Storage
{
    using System;
using System.Collections.Generic;

    public interface IStorage : IDisposable
    {
        void Store<T>(string key, T value);

        void BulkStore<T>(KeyValuePair<string, T>[] keysAndValues);

        void StoreOrUpdate<T>(string key, T value);

        void BulkStoreOrUpdate<T>(KeyValuePair<string, T>[] keysAndValues);

        void Update<T>(string key, T newValue);

        void BulkUpdate<T>(KeyValuePair<string, T>[] keysAndNewValues);

        T Retrieve<T>(string key);

        T[] BulkRetrieve<T>(string[] keys);

        void Remove(string key);

        void BulkRemove(string[] keys);
    }
}
