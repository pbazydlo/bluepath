namespace Bluepath.Storage
{
    using System;

    using Bluepath.Storage.Locks;

    public interface IExtendedStorage : IStorage
    {
        IStorageLock AcquireLock(string key);

        bool AcquireLock(string key, TimeSpan timeout, out IStorageLock storageLock);

        void ReleaseLock(IStorageLock storageLock);
    }
}
