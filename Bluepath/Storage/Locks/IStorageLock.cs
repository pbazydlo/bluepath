namespace Bluepath.Storage.Locks
{
    using System;

    public interface IStorageLock : IDisposable
    {
        string Key { get; }

        bool IsAcquired { get; }

        bool Acquire();
        
        bool Acquire(TimeSpan? timeout);

        void Release();
    }
}
