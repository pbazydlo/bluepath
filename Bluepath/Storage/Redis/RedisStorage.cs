namespace Bluepath.Storage.Redis
{
    using System;

    using Bluepath.Extensions;
    using Bluepath.Storage.Locks;

    using StackExchange.Redis;
    using Bluepath.Exceptions;

    [Serializable]
    public class RedisStorage : IExtendedStorage, IStorage
    {
        private const int ConnectRetryCount = 5;

        private string configurationString;

        [NonSerialized]
        private ConnectionMultiplexer connection;

        private object connectionLock = new object();

        private ConnectionMultiplexer Connection 
        {
            get 
            {
                lock (this.connectionLock)
                {
                    int retryNo = 0;
                    if (this.connection != null && this.connection.IsConnected == false)
                    {
                        Log.TraceMessage("There seems to be Redis connection failure, reseting connection.");
                        this.connection.Close();
                        this.connection = null;
                    }

                    while (this.connection == null && retryNo < ConnectRetryCount)
                    {
                        retryNo++;
                        try
                        {
                            Log.TraceMessage("There is no Redis connection available - establishing connection.");
                            this.connection = ConnectionMultiplexer.Connect(this.configurationString);
                        }
                        catch (TimeoutException ex)
                        {
                            this.connection = null;
                            Log.ExceptionMessage(ex, string.Format("Timeout retry no {0}", retryNo));
                        }
                    }
                }

                return this.connection;
            }
        }

        public RedisStorage(string configurationString)
        {
            this.configurationString = configurationString;
        }

        public void Store<T>(string key, T value)
        {
            if (!this.InternalStore(key, value, When.NotExists))
            {
                throw new StorageKeyAlreadyExistsException("key", string.Format("Such key[{0}] already exists!", key));
            }
        }

        public void StoreOrUpdate<T>(string key, T value)
        {
            if (!this.InternalStore(key, value, When.Always))
            {
                throw new StorageOperationException("Operation failed");
            }
        }

        public void Update<T>(string key, T newValue)
        {
            if (!this.InternalStore(key, newValue, When.Exists))
            {
                throw new StorageKeyDoesntExistException("key", string.Format("Such key[{0}] doesn't exist!", key));
            }
        }

        public T Retrieve<T>(string key)
        {
            var pendingResult = InternalRetrieve(key, ConnectRetryCount);

            return ((byte[])pendingResult.Result).Deserialize<T>();
        }

        private System.Threading.Tasks.Task<RedisValue> InternalRetrieve(string key, int retry)
        {
            var db = this.Connection.GetDatabase();
            var transaction = db.CreateTransaction();
            transaction.AddCondition(Condition.KeyExists(key));
            var pendingResult = transaction.StringGetAsync(key);
            try
            {
                var transactionSuccess = transaction.Execute();
                if (!transactionSuccess)
                {
                    throw new StorageKeyDoesntExistException("key", string.Format("Such key[{0}] doesn't exist!", key));
                }
            }
            catch (Exception ex)
            {
                if (ex is RedisConnectionException || ex is TimeoutException)
                {
                    Log.ExceptionMessage(ex, string.Format("Retrieve attempt no {0} for key '{1}' failed.", retry, key));
                    if ((ex.InnerException != null && ex.InnerException is OverflowException)
                        || ex is TimeoutException)
                    {
                        // pendingResult.Dispose();
                        if (retry <= 0)
                        {
                            throw new StorageOperationException("InternalRetrieve failed", ex);
                        }

                        return this.InternalRetrieve(key, retry - 1);
                    }
                }

                throw;
            }

            Log.TraceMessage("InternalRetrieve waits for result...");
            pendingResult.Wait();
            Log.TraceMessage("InternalRetrieve got result.");
            return pendingResult;
        }

        public void Remove(string key)
        {
            bool succededRemoving = false;
            do
            {
                var db = this.Connection.GetDatabase();
                var transaction = db.CreateTransaction();
                transaction.AddCondition(Condition.KeyExists(key));
                var pendingResult = transaction.KeyDeleteAsync(key);
                var transactionSuccess = transaction.Execute();
                if (!transactionSuccess)
                {
                    throw new StorageKeyDoesntExistException("key", string.Format("Such key[{0}] doesn't exist!", key));
                }

                pendingResult.Wait();
                succededRemoving = pendingResult.Result;
                if(!succededRemoving)
                {
                    Log.TraceMessage(string.Format("Removing key failed! [{0}]", key));
                }
            } while (!succededRemoving);
        }

        public IStorageLock AcquireLock(string key)
        {
            var storageLock = new RedisLock(this, key);
            storageLock.Acquire();
            return storageLock;
        }

        public bool AcquireLock(string key, TimeSpan timeout, out IStorageLock storageLock)
        {
            storageLock = new RedisLock(this, key);
            return storageLock.Acquire(timeout);
        }

        public void ReleaseLock(IStorageLock storageLock)
        {
            storageLock.Release();
        }

        public void Subscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handler)
        {
            var sub = this.Connection.GetSubscriber();
            sub.Subscribe(channel, handler);
        }

        public void Unsubscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handler)
        {
            var sub = this.Connection.GetSubscriber();
            sub.Unsubscribe(channel, handler);
        }

        public void Publish(RedisChannel channel, string message)
        {
            var sub = this.Connection.GetSubscriber();

            // TODO: this message could be potentially lost
            sub.Publish(channel, message, CommandFlags.None);
        }

        public void Dispose()
        {
            this.Connection.Dispose();
        }

        private bool InternalStore<T>(string key, T value, When when, int retry = ConnectRetryCount)
        {
            try
            {
                var db = this.Connection.GetDatabase();
                return db.StringSet(key, value.Serialize(), null, when);
            }
            catch(RedisConnectionException ex)
            {
                Log.ExceptionMessage(ex, string.Format("InternalStore failed, retry: {0}", retry));
                return this.InternalStore<T>(key, value, when, retry - 1);
            }
        }
    }
}
