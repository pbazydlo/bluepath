namespace Bluepath.Storage.Redis
{
    using System;

    using Bluepath.Extensions;
    using Bluepath.Storage.Locks;

    using StackExchange.Redis;

    public class RedisStorage : IExtendedStorage
    {
        private readonly ConnectionMultiplexer connection;

        public RedisStorage(string host)
        {
            this.connection = ConnectionMultiplexer.Connect(host);
        }

        public void Store<T>(string key, T value)
        {
            if (!this.InternalStore(key, value, When.NotExists))
            {
                throw new ArgumentOutOfRangeException("key", string.Format("Such key[{0}] already exists!", key));
            }
        }

        public void StoreOrUpdate<T>(string key, T value)
        {
            if (!this.InternalStore(key, value, When.Always))
            {
                throw new Exception("Operation failed");
            }
        }

        public void Update<T>(string key, T newValue)
        {
            if (!this.InternalStore(key, newValue, When.Exists))
            {
                throw new ArgumentOutOfRangeException("key", string.Format("Such key[{0}] doesn't exist!", key));
            }
        }

        public T Retrieve<T>(string key)
        {
            var db = this.connection.GetDatabase();
            var transaction = db.CreateTransaction();
            transaction.AddCondition(Condition.KeyExists(key));
            var pendingResult = transaction.StringGetAsync(key);
            var transactionSuccess = transaction.Execute();
            if (!transactionSuccess)
            {
                throw new ArgumentOutOfRangeException("key", string.Format("Such key[{0}] doesn't exist!", key));
            }

            pendingResult.Wait();

            return ((byte[])pendingResult.Result).Deserialize<T>();
        }

        public void Remove(string key)
        {
            var db = this.connection.GetDatabase();
            var transaction = db.CreateTransaction();
            transaction.AddCondition(Condition.KeyExists(key));
            var pendingResult = transaction.KeyDeleteAsync(key);
            var transactionSuccess = transaction.Execute();
            if (!transactionSuccess)
            {
                throw new ArgumentOutOfRangeException("key", string.Format("Such key[{0}] doesn't exist!", key));
            }

            pendingResult.Wait();
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
            var sub = this.connection.GetSubscriber();
            sub.Subscribe(channel, handler);
        }

        public void Unsubscribe(RedisChannel channel, Action<RedisChannel, RedisValue> handler)
        {
            var sub = this.connection.GetSubscriber();
            sub.Unsubscribe(channel, handler);
        }

        public void Publish(RedisChannel channel, string message)
        {
            var sub = this.connection.GetSubscriber();

            // TODO: this message could be potentially lost
            sub.Publish(channel, message, CommandFlags.FireAndForget);
        }

        public void Dispose()
        {
            this.connection.Dispose();
        }

        private bool InternalStore<T>(string key, T value, When when)
        {
            var db = this.connection.GetDatabase();
            return db.StringSet(key, value.Serialize(), null, when);
        }
    }
}
