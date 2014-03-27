using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluepath.Extensions;

namespace Bluepath.Storage
{
    public class RedisStorage : IStorage, IDisposable
    {
        private ConnectionMultiplexer connection;

        public RedisStorage(string host)
        {
            this.connection = ConnectionMultiplexer.Connect(host);
        }

        public void Store<T>(string key, T value)
        {
            if(!this.InternalStore(key, value, When.NotExists))
            {
                throw new ArgumentOutOfRangeException("key", string.Format("Such key[{0}] already exists!", key));
            }
        }

        public void StoreOrUpdate<T>(string key, T value)
        {
            if(!this.InternalStore(key, value, When.Always))
            {
                throw new Exception("Operation failed");
            }
        }

        public void Update<T>(string key, T newValue)
        {
            if(!this.InternalStore(key, newValue, When.Exists))
            {
                throw new ArgumentOutOfRangeException("key", string.Format("Such key[{0}] doesn't exist!", key));
            }
        }

        public T Retrieve<T>(string key)
        {
            var db = this.connection.GetDatabase();
            var transaction = db.CreateTransaction();
            transaction.AddCondition(Condition.KeyExists(key));
            var awaitableResult = transaction.StringGetAsync(key);
            var transactionSuccess = transaction.Execute();
            if(!transactionSuccess)
            {
                throw new ArgumentOutOfRangeException("key", string.Format("Such key[{0}] doesn't exist!", key));
            }

            awaitableResult.Wait();
            
            return ((byte[])awaitableResult.Result).Deserialize<T>();
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
