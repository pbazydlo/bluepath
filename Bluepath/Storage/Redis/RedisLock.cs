namespace Bluepath.Storage.Redis
{
    using System;
    using System.Threading;

    using Bluepath.Storage.Locks;

    using StackExchange.Redis;

    public class RedisLock : IStorageLock
    {
        private const string LockKeyPrefix = "_lock_";
        private const string LockChannelPrefix = "_lockChannel_";
        private readonly object acquireLock = new object();
        private readonly string key;
        private readonly RedisStorage redisStorage;
        private bool isAcquired;
        private bool wasPulsed;

        public RedisLock(RedisStorage redisStorage, string key)
        {
            this.redisStorage = redisStorage;
            this.key = key;
        }

        public string Key
        {
            get { return this.key; }
        }

        public bool IsAcquired
        {
            get { return this.isAcquired; }
        }

        public bool Acquire()
        {
            return this.Acquire(null);
        }

        private string LockKey
        {
            get
            {
                return string.Format("{0}{1}", LockKeyPrefix, this.key);
            }
        }

        private string LockChannel
        {
            get
            {
                return string.Format("{0}{1}", LockChannelPrefix, this.key);
            }
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
            this.redisStorage.Subscribe(this.LockChannel, this.ChannelPulse);
            lock (this.acquireLock)
            {
                do
                {
                    try
                    {
                        this.redisStorage.Store(this.LockKey, 1);
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
                                    this.redisStorage.Unsubscribe(this.LockChannel, this.ChannelPulse);
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
                }
                while (!wasLockAcquired);
            }

            this.redisStorage.Unsubscribe(this.LockChannel, this.ChannelPulse);
            this.isAcquired = true;
            return true;
        }

        public void Release()
        {
            this.redisStorage.Remove(this.LockKey);
            this.redisStorage.Publish(this.LockChannel, "release");
        }

        public void Dispose()
        {
            if (this.IsAcquired)
            {
                this.Release();
            }
        }

        private void ChannelPulse(RedisChannel redisChannel, RedisValue redisValue)
        {
            lock (this.acquireLock)
            {
                this.wasPulsed = true;
                Monitor.Pulse(this.acquireLock);
            }
        }


        public void Wait()
        {
            throw new NotImplementedException();
            this.Release();
            this.Acquire();
        }

        public void Wait(TimeSpan? timeout)
        {
            throw new NotImplementedException();
            this.Release();
            // wait no longer than timeout
            this.Acquire();
        }

        public void Pulse()
        {
            throw new NotImplementedException();
        }

        public void PulseAll()
        {
            throw new NotImplementedException();
        }
    }
}
