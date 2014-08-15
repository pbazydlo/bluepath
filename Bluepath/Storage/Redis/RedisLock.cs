namespace Bluepath.Storage.Redis
{
    using System;
    using System.Threading;

    using Bluepath.Storage.Locks;

    using StackExchange.Redis;
    using Bluepath.Exceptions;

    public class RedisLock : IStorageLock
    {
        private const string LockKeyPrefix = "_lock_";
        private const string LockChannelPrefix = "_lockChannel_";
        private const string WaitChannelPrefix = "_lockWaitChannel_";
        private const string PulseFilePrefix = "_lockPulseFile_";
        private readonly object acquireLock = new object();
        private readonly object waitLock = new object();
        private readonly string key;
        private readonly IExtendedStorage redisStorage;
        private bool isAcquired;
        private bool wasPulsed;
        private bool wasWaitPulsed;

        public RedisLock(IExtendedStorage redisStorage, string key)
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
                return this.ApplyPrefix(LockKeyPrefix);
            }
        }

        private string LockChannel
        {
            get
            {
                return this.ApplyPrefix(LockChannelPrefix);
            }
        }

        private string WaitChannel
        {
            get
            {
                return this.ApplyPrefix(WaitChannelPrefix);
            }
        }

        /// <summary>
        /// File that needs to be removed (succesfully) if we want to wake on pulse.
        /// </summary>
        private string PulseFile
        {
            get
            {
                return this.ApplyPrefix(PulseFilePrefix);
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
                    catch (StorageKeyAlreadyExistsException)
                    {
                        this.wasPulsed = false;

                        int retryNo = 0;
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

                            retryNo++;
                            if(retryNo > 5)
                            {
                                break;
                            }
                        }
                    }
                }
                while (!wasLockAcquired);
            }

            this.redisStorage.Unsubscribe(this.LockChannel, this.ChannelPulse);
            this.isAcquired = true;
            Log.TraceMessage(Log.Activity.Info,string.Format("Lock acquired key: '{0}'", this.LockKey), logLocallyOnly: true);
            return true;
        }

        public void Release()
        {
            this.isAcquired = false;
            this.redisStorage.Remove(this.LockKey);
            try
            {
                this.redisStorage.Retrieve<int>(this.LockKey);
                Log.TraceMessage(Log.Activity.Info,string.Format("Key wasn't removed [{0}]", this.LockKey), logLocallyOnly: true);
            }
            catch(StorageKeyDoesntExistException)
            {

            }

            this.redisStorage.Publish(this.LockChannel, "release");
            Log.TraceMessage(Log.Activity.Info,string.Format("Lock released key: '{0}'", this.LockKey), logLocallyOnly: true);
        }

        public void Dispose()
        {
            if (this.IsAcquired)
            {
                this.Release();
            }
        }

        private void ChannelPulse(object redisChannel, object redisValue)
        {
            lock (this.acquireLock)
            {
                this.wasPulsed = true;
                Monitor.Pulse(this.acquireLock);
            }
        }

        private void WaitChannelPulse(object redisChannel, object redisValue)
        {
            lock (this.waitLock)
            {
                if (this.wasWaitPulsed)
                {
                    // somehow we got message that we shouldn't get
                    return;
                }

                try
                {
                    this.wasWaitPulsed = true;
                    var pulseType = (PulseType)(int.Parse(redisValue.ToString()));
                    if (pulseType == PulseType.One)
                    {
                        // try get permission to wake
                        try
                        {
                            this.redisStorage.Remove(this.PulseFile);
                        }
                        catch (StorageKeyDoesntExistException)
                        {
                            // failed - need to sleep longer
                            return;
                        }
                    }

                    // unsubscribe yourself
                    this.redisStorage.Unsubscribe(this.WaitChannel, this.WaitChannelPulse);
                    Monitor.Pulse(this.waitLock);
                }
                finally
                {
                    this.wasWaitPulsed = false;
                }
            }
        }

        public void Wait()
        {
            this.Wait(null);
        }

        public void Wait(TimeSpan? timeout)
        {
            this.wasWaitPulsed = false;
            this.redisStorage.Subscribe(this.WaitChannel, this.WaitChannelPulse);
            var waitThread = new Thread(() =>
            {
                lock (this.waitLock)
                {
                    if (timeout.HasValue)
                    {
                        Monitor.Wait(this.waitLock, timeout.Value);
                    }
                    else
                    {
                        Monitor.Wait(this.waitLock);
                    }
                }
            });
            waitThread.Start();
            
            this.Release();
            waitThread.Join();
            this.Acquire();
        }

        public void Pulse()
        {
            this.redisStorage.Store(this.PulseFile, "pulsed");
            this.PublishPulse(PulseType.One);
        }

        public void PulseAll()
        {
            this.PublishPulse(PulseType.All);
        }

        private void PublishPulse(PulseType pulseType)
        {
            this.redisStorage.Publish(this.WaitChannel, ((int)pulseType).ToString());
        }

        private string ApplyPrefix(string prefix)
        {
            return string.Format("{0}{1}", prefix, this.key);
        }

        private enum PulseType
        {
            One,
            All
        }
    }
}
