namespace Bluepath.Storage
{
    using System;

    using Bluepath.Storage.Locks;

    using StackExchange.Redis;

    public interface IExtendedStorage : IStorage
    {
        IStorageLock AcquireLock(string key);

        bool AcquireLock(string key, TimeSpan timeout, out IStorageLock storageLock);

        void ReleaseLock(IStorageLock storageLock);

        void Subscribe(object channel, Action<object, object> channelPulse);

        void Unsubscribe(object channel, Action<object, object> handler);

        void Publish(object channel, string message);
    }
}