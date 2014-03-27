namespace Bluepath.Storage
{
    using System;

    public interface IStorage : IDisposable
    {
        void Store<T>(string key, T value);

        void StoreOrUpdate<T>(string key, T value);

        void Update<T>(string key, T newValue);

        T Retrieve<T>(string key);

        void Remove(string key);
    }
}
