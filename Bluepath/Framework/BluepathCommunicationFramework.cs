namespace Bluepath.Framework
{
    using System;

    using Bluepath.Executor;
    using Bluepath.Storage;
    using Bluepath.Storage.Locks;

    public class BluepathCommunicationFramework : IBluepathCommunicationFramework
    {
        private readonly ILocalExecutor executor;

        public BluepathCommunicationFramework()
        {
        }

        public BluepathCommunicationFramework(ILocalExecutor executor)
        {
            this.executor = executor;
        }

        public Guid ProcessEid
        {
            get
            {
                if (this.executor == null)
                {
                    throw new Exception("Current BluepathCommunicationFramework object is not bound to the local executor and this information is unavailable.");
                }

                return this.executor.Eid;
            }
        }

        public IStorage Storage { get; private set; }

        // TODO: Provide locks
        // Could be based on:
        //  Apache Zookeeper [https://github.com/ewhauser/zookeeper/tree/trunk/src/dotnet]
        //  Redis [https://github.com/ServiceStack/ServiceStack.Redis/wiki/RedisLocks] - s1 said that it is easy to deploy
        //  ZeroMQ [http://zeromq.org/]
        public IStorageLock AcquireLock(string key)
        {
            if (this.Storage is IExtendedStorage)
            {
                return (this.Storage as IExtendedStorage).AcquireLock(key);
            }

            throw new Exception("Available storage does not provide IExtendedStorage capabilities");
        }

        public bool AcquireLock(string key, TimeSpan timeout, out IStorageLock storageLock)
        {
            if (this.Storage is IExtendedStorage)
            {
                return (this.Storage as IExtendedStorage).AcquireLock(key, timeout, out storageLock);
            }

            throw new Exception("Available storage does not provide IExtendedStorage capabilities");
        }

        public void ReleaseLock(IStorageLock storageLock)
        {
            if (this.Storage is IExtendedStorage)
            {
                (this.Storage as IExtendedStorage).ReleaseLock(storageLock);
            }

            throw new Exception("Available storage does not provide IExtendedStorage capabilities");
        }
    }
}
