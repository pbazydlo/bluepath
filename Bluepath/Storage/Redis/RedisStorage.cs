namespace Bluepath.Storage.Redis
{
    using System;
    using System.Linq;

    using Bluepath.Extensions;
    using Bluepath.Storage.Locks;

    using StackExchange.Redis;
    using Bluepath.Exceptions;
    using System.Collections.Generic;

    [Serializable]
    public class RedisStorage : IExtendedStorage, IStorage
    {
        private const int ConnectRetryCount = 15;
        internal static TimeSpan LockTimespan = TimeSpan.FromSeconds(5);

        private string configurationString;

        [NonSerialized]
        private static ConnectionMultiplexer connection;

        private static object connectionLock = new object();

        private ConnectionMultiplexer Connection
        {
            get
            {
                lock (connectionLock)
                {
                    int retryNo = 0;
                    if (connection != null && connection.IsConnected == false)
                    {
                        Log.TraceMessage(Log.Activity.Info, "There seems to be Redis connection failure, reseting connection.", logLocallyOnly: true);
                        connection.Close();
                        connection.Dispose();
                        connection = null;
                    }

                    while (connection == null && retryNo < ConnectRetryCount)
                    {
                        retryNo++;
                        try
                        {
                            Log.TraceMessage(Log.Activity.Info, "There is no Redis connection available - establishing connection.", logLocallyOnly: true);
                            var config = ConfigurationOptions.Parse(this.configurationString);
                            config.ConnectTimeout = 10000;
                            //config.KeepAlive = 1;
                            config.SyncTimeout = 30000;
                            config.ConnectRetry = 5;
                            config.AbortOnConnectFail = true;
                            config.ResolveDns = true;
                            connection = ConnectionMultiplexer.Connect(config);
                            while (!connection.IsConnected)
                            {
                                System.Threading.Thread.Sleep(100);
                            }
                        }
                        catch (Exception ex)
                        {
                            connection = null;
                            Log.ExceptionMessage(ex, Log.Activity.Info, string.Format("Timeout retry no {0}", retryNo), logLocallyOnly: true);
                        }
                    }
                }

                return connection;
            }
        }

        public RedisStorage(string configurationString)
        {
            this.configurationString = configurationString;
        }

        public void Store<T>(string key, T value)
        {
            var keyAndValue = new KeyValuePair<string, T>[] { new KeyValuePair<string, T>(key, value) };
            if (!this.InternalStore(keyAndValue, When.NotExists))
            {
                throw new StorageKeyAlreadyExistsException("key", string.Format("Such key[{0}] already exists!", key));
            }
        }

        public void StoreOrUpdate<T>(string key, T value)
        {
            var keyAndValue = new KeyValuePair<string, T>[] { new KeyValuePair<string, T>(key, value) };
            if (!this.InternalStore(keyAndValue, When.Always))
            {
                throw new StorageOperationException("Operation failed");
            }
        }

        public void Update<T>(string key, T newValue)
        {
            var keyAndValue = new KeyValuePair<string, T>[] { new KeyValuePair<string, T>(key, newValue) };
            if (!this.InternalStore(keyAndValue, When.Exists))
            {
                throw new StorageKeyDoesntExistException("key", string.Format("Such key[{0}] doesn't exist!", key));
            }
        }

        public T Retrieve<T>(string key)
        {
            return ((byte[])this.InternalRetrieve(new string[] { key }, ConnectRetryCount)[0]).Deserialize<T>();
        }

        private RedisValue[] InternalRetrieve(string[] keys, int retry)
        {
            if(keys.Length==0)
            {
                return new RedisValue[0];
            }

            var db = this.Connection.GetDatabase();
            RedisValue[] values = null;
            var stringGetTask = db.StringGetAsync(keys.Select(k => (RedisKey)k).ToArray());
            try
            {
                stringGetTask.Wait();
                values = stringGetTask.Result;
            }
            catch (AggregateException agEx)
            {
                throw agEx.InnerExceptions.FirstOrDefault();
            }

            if (values.Any(v => v.IsNull))
            {
                var wrongKeys = keys.Where(k => values[Array.IndexOf(keys, k)].IsNull).ToArray();
                throw new StorageKeyDoesntExistException("key", string.Format("Such keys[{0}] don't exist!", string.Join(";", wrongKeys)));
            }

            return values;
        }

        public void Remove(string key)
        {
            this.InternalRemove(new string[] { key });

            Log.TraceMessage(Log.Activity.Info, string.Format("Removed key '{0}'", key), logLocallyOnly: true);
        }

        private void InternalRemove(string[] keys)
        {
            // TODO implement as bulk operation
            foreach (var key in keys)
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
                    if (!succededRemoving)
                    {
                        Log.TraceMessage(Log.Activity.Info, string.Format("Removing key failed! [{0}]", key), logLocallyOnly: true);
                    }
                } while (!succededRemoving);
            }
        }

        public IStorageLock AcquireLock(string key)
        {
            var storageLock = new NativeRedisLock(this, key);
            storageLock.Acquire();
            return storageLock;
        }

        public bool AcquireLock(string key, TimeSpan timeout, out IStorageLock storageLock)
        {
            storageLock = new NativeRedisLock(this, key);
            return storageLock.Acquire(timeout);
        }

        public void ReleaseLock(IStorageLock storageLock)
        {
            storageLock.Release();
        }

        public void Subscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handler)
        {
            try
            {
                var sub = this.Connection.GetSubscriber();
                sub.Subscribe(channel, handler);
            }
            catch (TimeoutException)
            {
                this.Subscribe(channel, handler);
            }
        }

        public void Unsubscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handler)
        {
            try
            {
                var sub = this.Connection.GetSubscriber();
                sub.Unsubscribe(channel, handler);
            }
            catch (TimeoutException)
            {
                this.Unsubscribe(channel, handler);
            }
        }

        public void Publish(RedisChannel channel, string message)
        {
            var sub = this.Connection.GetSubscriber();

            // TODO: this message could be potentially lost
            sub.Publish(channel, message, CommandFlags.None);
        }

        public bool LockTake(string key, string value)
        {
            try
            {
                var db = this.Connection.GetDatabase();
                return db.LockTake(key, value, LockTimespan);
            }
            catch (Exception ex)
            {
                Log.ExceptionMessage(ex, Log.Activity.Info);
                return false;
            }
        }

        public bool LockExtend(string key, string value)
        {
            var db = this.Connection.GetDatabase();
            return db.LockExtend(key, value, LockTimespan);
        }

        public bool LockRelease(string key, string value)
        {
            try
            {
                var db = this.Connection.GetDatabase();
                return db.LockRelease(key, value);
            }
            catch (Exception ex)
            {
                Log.ExceptionMessage(ex, Log.Activity.Info);
                return false;
            }
        }

        public RedisValue LockQuery(string key)
        {
            var db = this.Connection.GetDatabase();
            return db.LockQuery(key);
        }

        public void Dispose()
        {
            // this.Connection.Dispose();
        }

        private bool InternalStore<T>(KeyValuePair<string, T>[] keysAndValues, When when, int retry = ConnectRetryCount)
        {
            if(keysAndValues.Length==0)
            {
                return true;
            }

            try
            {
                var db = this.Connection.GetDatabase();
                var redisKeysAndValues = keysAndValues.Select(kv => new KeyValuePair<RedisKey, RedisValue>(kv.Key, kv.Value.Serialize())).ToArray();
                var stringSetTask = db.StringSetAsync(redisKeysAndValues, when);
                try
                {
                    stringSetTask.Wait();
                    return stringSetTask.Result;
                }
                catch(AggregateException agEx)
                {
                    throw agEx.InnerExceptions.FirstOrDefault();
                }
            }
            catch (RedisConnectionException ex)
            {
                Log.ExceptionMessage(ex, Log.Activity.Info, string.Format("InternalStore failed, retry: {0}", retry), logLocallyOnly: true);
                if (retry >= 0)
                {
                    return this.InternalStore<T>(keysAndValues, when, retry - 1);
                }

                throw;
            }
        }

        public void BulkStore<T>(KeyValuePair<string, T>[] keysAndValues)
        {
            if (!this.InternalStore(keysAndValues, When.NotExists))
            {
                throw new StorageKeyAlreadyExistsException("keysAndValues", "Some keys already exist.");
            }
        }

        public void BulkStoreOrUpdate<T>(KeyValuePair<string, T>[] keysAndValues)
        {
            if (!this.InternalStore(keysAndValues, When.Always))
            {
                throw new StorageOperationException("Operation failed");
            }
        }

        public void BulkUpdate<T>(KeyValuePair<string, T>[] keysAndNewValues)
        {
            if (!this.InternalStore(keysAndNewValues, When.Exists))
            {
                throw new StorageKeyDoesntExistException("keysAndNewValues", "Some of keys dosn't exist.");
            }
        }

        public T[] BulkRetrieve<T>(string[] keys)
        {
            return this.InternalRetrieve(keys, ConnectRetryCount).Select(v => ((byte[])v).Deserialize<T>()).ToArray();
        }

        public void BulkRemove(string[] keys)
        {
            this.InternalRemove(keys);

            Log.TraceMessage(Log.Activity.Info, string.Format("Removed keys '{0}'", string.Join(";", keys)), logLocallyOnly: true);
        }
    }
}
