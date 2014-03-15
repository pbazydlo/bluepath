namespace Bluepath.Storage
{
    using System;
    using System.Linq;

    using Bluepath.Extensions;

    using Rhino.DistributedHashTable.Client;
    using Rhino.DistributedHashTable.Client.Pooling;
    using Rhino.DistributedHashTable.Hosting;
    using Rhino.PersistentHashTable;

    public class RhinoDhtStorage : IStorage, IDisposable
    {
        private Uri masterUri;
        private bool isMaster;
        private DistributedHashTableMasterHost masterHost;
        private DistributedHashTableStorageHost storageHost;
        private DistributedHashTableMasterClient masterClient;
        private DistributedHashTableStorageClient storageClient;
        private DistributedHashTable dht;

        public RhinoDhtStorage(string masterIp = "localhost", int masterPort = 2200, bool isMaster = true)
        {
            this.masterUri = new Uri(string.Format("rhino.dht://{0}:{1}", masterIp, masterPort));
            this.isMaster = isMaster;
            if (this.isMaster)
            {
                this.masterHost = new DistributedHashTableMasterHost();
                this.masterHost.Start();
                this.InitializeStorageHost();
            }
            else
            {
                this.InitializeStorageHost();
            }
        }

        public void Store<T>(string key, T value)
        {
            try
            {
                this.Retrieve<object>(key);
                throw new ArgumentException(string.Format("Given key ({0}) already exists!", key), "key");
            }
            catch (ArgumentOutOfRangeException)
            {
                this.InternalStore(key, value.Serialize());
            }
        }

        public void StoreOrUpdate<T>(string key, T value)
        {
            this.InternalStore(key, value.Serialize());
        }

        public void Update<T>(string key, T newValue)
        {
            var values = this.InternalRetrieve(key);
            var parentVersions = values.Select(v => v.Version)
                .OrderBy(v => v.Number).ToArray();
            this.InternalStore(key, newValue.Serialize(), parentVersions);
        }

        public T Retrieve<T>(string key)
        {
            var values = this.InternalRetrieve(key);
            var maxVersionNo = values.Max(v => v.Version.Number);
            var mostRecentValue = values.First(v => v.Version.Number == maxVersionNo);
            return mostRecentValue.Data.Deserialize<T>();
        }

        public void Dispose()
        {
            if (this.masterHost != null)
            {
                this.masterHost.Dispose();
            }

            if (this.storageHost != null)
            {
                this.storageHost.Dispose();
            }
        }

        private Value[] InternalRetrieve(string key)
        {
            var values = this.dht.Get(new GetRequest()
            {
                Key = key
            });

            if (values.Length == 0 || values[0].Length == 0)
            {
                throw new ArgumentOutOfRangeException("key", string.Format("Value with given key ({0}) doesn't exist in the storage!", key));
            }

            return values[0];
        }

        private void InitializeStorageHost()
        {
            this.storageHost = new DistributedHashTableStorageHost(this.masterUri);
            this.storageHost.Start();
            this.masterClient = new DistributedHashTableMasterClient(this.masterUri);
            this.dht = new DistributedHashTable(this.masterClient, new DefaultConnectionPool());
        }

        private void InternalStore(string key, byte[] value, ValueVersion[] parentVersions = null)
        {
            this.dht.Put(new PutRequest()
            {
                Key = key,
                Bytes = value,
                ParentVersions = parentVersions ?? new ValueVersion[0]
            });
        }
    }
}
