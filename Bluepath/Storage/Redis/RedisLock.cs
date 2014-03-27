using Bluepath.Storage.Locks;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bluepath.Storage.Redis
{
    public class RedisLock : IStorageLock
    {
        private const string LockKeyPrefix = "_lock_";
        private const string LockChannelPrefix = "_lockChannel_";
        private RedisStorage redisStorage;
        private bool isAcquired;
        private object acquireLock = new object();
        private string key;
        private bool wasPulsed;
        private string lockKey
        {
            get
            {
                return string.Format("{0}{1}", LockKeyPrefix, key);
            }
        }

        private string lockChannel
        {
            get
            {
                return string.Format("{0}{1}", LockChannelPrefix, key);
            }
        }

        public RedisLock(RedisStorage redisStorage, string key)
        {
            this.redisStorage = redisStorage;
            this.key = key;
        }

        public bool IsAcquired
        {
            get { return this.isAcquired; }
        }

        private void channelPulse(RedisChannel redisChannel, RedisValue redisValue)
        {
            lock (this.acquireLock)
            {
                this.wasPulsed = true;
                Monitor.Pulse(this.acquireLock);
            }
        }

        public bool Acquire()
        {
            return this.Acquire(null);
        }

        public bool Acquire(TimeSpan? timeout)
        {
            if (this.IsAcquired)
            {
                throw new InvalidOperationException(string.Format("This lock[{0}] is alreay acquired!", this.Key));
            }

            var start = DateTime.Now;
            bool isFirstWait = true;
            this.wasPulsed = false;
            bool wasLockAcquired = false;
            this.redisStorage.Subscribe(this.lockChannel, this.channelPulse);
            lock (this.acquireLock)
            {
                do
                {
                    try
                    {
                        this.redisStorage.Store(this.lockKey, 1);
                        wasLockAcquired = true;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        this.wasPulsed = false;

                        // lock is already acquired
                        while (!this.wasPulsed)
                        {
                            if (timeout.HasValue)
                            {
                                if ((DateTime.Now - start) > timeout.Value)
                                {
                                    this.redisStorage.Unsubscribe(this.lockChannel, this.channelPulse);
                                    return false;
                                }
                            }

                            if (isFirstWait)
                            {
                                isFirstWait = false;
                                Monitor.Wait(this.acquireLock, 10);
                            }
                            else
                            {
                                Monitor.Wait(this.acquireLock, timeout ?? TimeSpan.FromMilliseconds(1000));
                            }
                        }
                    }
                } while (!wasLockAcquired);
            }

            this.redisStorage.Unsubscribe(this.lockChannel, this.channelPulse);
            this.isAcquired = true;
            return true;
        }

        public void Release()
        {
            this.redisStorage.Remove(this.lockKey);
            this.redisStorage.Publish(this.lockChannel, "release");
        }

        public void Dispose()
        {
            if(this.IsAcquired)
            {
                this.Release();
            }
        }

        public string Key
        {
            get { return this.key; }
        }
    }
}
