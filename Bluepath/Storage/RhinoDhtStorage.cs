namespace Bluepath.Storage
{
    using System;
    using System.Linq;

    using Bluepath.Extensions;

    using Rhino.DistributedHashTable.Client;
    using Rhino.DistributedHashTable.Client.Pooling;
    using Rhino.DistributedHashTable.Hosting;
    using Rhino.PersistentHashTable;

    public class RhinoDhtStorage : IStorage
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
            if(this.isMaster)
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

        private void InitializeStorageHost()
        {
            this.storageHost = new DistributedHashTableStorageHost(this.masterUri);
            this.storageHost.Start();
            this.masterClient = new DistributedHashTableMasterClient(this.masterUri);
            this.dht = new DistributedHashTable(this.masterClient, new DefaultConnectionPool());
        }

        public void Store<T>(string key, T value)
        {
            // TODO: Should throw exception if object exists
            this.StoreOrUpdate(key, value);
        }

        public void StoreOrUpdate<T>(string key, T value)
        {
            
            this.dht.Put(new PutRequest()
            {
                Key = key,
                Bytes = value.Serialize(),
                ParentVersions = new ValueVersion[0]
            });
        }

        public void Update<T>(string key, T newValue)
        {
            // TODO: Should throw exception if object doesn't exist
            this.StoreOrUpdate(key, newValue);
        }

        public T Retrieve<T>(string key)
        {
            var values = this.dht.Get(new GetRequest()
                {
                    Key = key
                });
            if(values.Length==0)
            {
                throw new ArgumentOutOfRangeException("key", string.Format("Value with given key ({0}) doesn't exist in the storage!", key));
            }

            var maxVersionNo = values[0].Max(v => v.Version.Number);
            var mostRecentValue = values[0].First(v => v.Version.Number == maxVersionNo);
            return mostRecentValue.Data.Deserialize<T>();
        }
    }
}
