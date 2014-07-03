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
        private const int ConnectRetryCount = 15;
        private static TimeSpan LockTimespan = TimeSpan.FromSeconds(10);

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
                        Log.TraceMessage(Log.Activity.Info,"There seems to be Redis connection failure, reseting connection.", logLocallyOnly: true);
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
                            while(!connection.IsConnected)
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
            //var pendingResult = InternalRetrieve(key, ConnectRetryCount);

            //return ((byte[])pendingResult.Result).Deserialize<T>();
            return ((byte[])this.InternalRetrieve(key, ConnectRetryCount)).Deserialize<T>();
        }

        private RedisValue InternalRetrieve(string key, int retry)
        {
            var db = this.Connection.GetDatabase();
            var value = db.StringGet(key);
            if(value.IsNull)
            {
                throw new StorageKeyDoesntExistException("key", string.Format("Such key[{0}] doesn't exist!", key));
            }

            return value;
            //var db = this.Connection.GetDatabase();
            //var transaction = db.CreateTransaction();
            //transaction.AddCondition(Condition.KeyExists(key));
            //var pendingResult = transaction.StringGetAsync(key);
            //try
            //{
            //    var transactionSuccess = transaction.Execute();
            //    if (!transactionSuccess)
            //    {
            //        throw new StorageKeyDoesntExistException("key", string.Format("Such key[{0}] doesn't exist!", key));
            //    }
            //}
            //catch (Exception ex)
            //{
            //    if (ex is RedisConnectionException || ex is TimeoutException)
            //    {
            //        Log.ExceptionMessage(ex, Log.Activity.Info, string.Format("Retrieve attempt no {0} for key '{1}' failed.", retry, key), logLocallyOnly: true);
            //        //if ((ex.InnerException != null && ex.InnerException is OverflowException)
            //        //    || ex is TimeoutException)
            //        //{
            //        // pendingResult.Dispose();
            //        if (retry <= 0)
            //        {
            //            throw new StorageOperationException("InternalRetrieve failed", ex);
            //        }

            //        return this.InternalRetrieve(key, retry - 1);
            //        //}
            //    }

            //    throw;
            //}

            //Log.TraceMessage(Log.Activity.Info, "InternalRetrieve waits for result...", logLocallyOnly: true);
            //pendingResult.Wait();
            //Log.TraceMessage(Log.Activity.Info, "InternalRetrieve got result.", logLocallyOnly: true);
            //return pendingResult;
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
                if (!succededRemoving)
                {
                    Log.TraceMessage(Log.Activity.Info,string.Format("Removing key failed! [{0}]", key), logLocallyOnly: true);
                }
            } while (!succededRemoving);

            Log.TraceMessage(Log.Activity.Info,string.Format("Removed key '{0}'", key), logLocallyOnly: true);
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
            catch(Exception ex)
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

        private bool InternalStore<T>(string key, T value, When when, int retry = ConnectRetryCount)
        {
            try
            {
                var db = this.Connection.GetDatabase();
                return db.StringSet(key, value.Serialize(), null, when);
            }
            catch (RedisConnectionException ex)
            {
                Log.ExceptionMessage(ex, Log.Activity.Info, string.Format("InternalStore failed, retry: {0}", retry), logLocallyOnly: true);
                if (retry >= 0)
                {
                    return this.InternalStore<T>(key, value, when, retry - 1);
                }

                throw;
            }
        }
    }
}
