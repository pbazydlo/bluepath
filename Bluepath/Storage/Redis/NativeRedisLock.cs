using Bluepath.Exceptions;
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
    public class NativeRedisLock : IStorageLock
    {
        private const string LockKeyPrefix = "_lock_";
        private const string LockChannelPrefix = "_lockChannel_";
        private const string WaitChannelPrefix = "_lockWaitChannel_";
        private const string PulseFilePrefix = "_lockPulseFile_";
        private readonly object acquireLock = new object();
        private readonly object waitLock = new object();
        private RedisStorage redisStorage;
        private string localLockIdentifier;
        private bool wasWaitPulsed;
        private Thread holdLockThread;

        public NativeRedisLock(RedisStorage redisStorage, string key)
        {
            this.localLockIdentifier = Guid.NewGuid().ToString();
            this.redisStorage = redisStorage;
            this.Key = key;
        }

        public string Key
        {
            get;
            private set;
        }

        public string LockKey
        {
            get { return this.ApplyPrefix(LockKeyPrefix); }
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

        public bool IsAcquired
        {
            get;
            private set;
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

            this.redisStorage.Subscribe(this.LockChannel, this.ChannelPulse);
            var start = DateTime.Now;
            while (!this.IsAcquired)
            {
                lock (this.acquireLock)
                {
                    if (timeout.HasValue && (DateTime.Now - start) > timeout.Value)
                    {
                        return false;
                    }

                    this.IsAcquired = this.redisStorage.LockTake(this.LockKey, this.localLockIdentifier);
                    if (!this.IsAcquired)
                    {
                        Monitor.Wait(this.acquireLock, timeout.HasValue ? timeout.Value : TimeSpan.FromMilliseconds(100));
                    }
                }
            }

            // TODO we need to start lock renew thread (lock is taken for 1s by default) - if we lose lock we need to throw exception on main thread
            

            this.redisStorage.Unsubscribe(this.LockChannel, this.ChannelPulse);
            this.holdLockThread = new Thread(() => 
            {
                var reacquireSleepMiliseconds = (int)Math.Floor(RedisStorage.LockTimespan.TotalMilliseconds / 2);
                if(reacquireSleepMiliseconds==0)
                {
                    reacquireSleepMiliseconds=1;
                }

                while(this.IsAcquired)
                {
                    Thread.Sleep(reacquireSleepMiliseconds);
                    lock(this.acquireLock)
                    {
                        if(!this.IsAcquired)
                        {
                            break;
                        }

                        if(!this.RenewLock())
                        {
                            break;
                        }
                    }
                }
            });
            this.holdLockThread.Start();

            Log.TraceMessage(Log.Activity.Info, string.Format("Lock '{1}' acquired key: '{0}'", this.LockKey, this.localLockIdentifier), logLocallyOnly: true);
            return this.IsAcquired;
        }

        public void Release()
        {
            if (!this.holdLockThread.IsAlive)
            {
                throw new LostLockBeforeReleaseException();
            }

            lock (this.acquireLock)
            {
                this.IsAcquired = false;
            }

            // this.holdLockThread.Join(); -- commented out to improve performance, correctness should be provided by lock on this.IsAcquired
            this.redisStorage.LockRelease(this.LockKey, this.localLockIdentifier);
            this.redisStorage.Publish(this.LockChannel, "release");
            Log.TraceMessage(Log.Activity.Info, string.Format("Lock '{1}' released key: '{0}'", this.LockKey, this.localLockIdentifier), logLocallyOnly: true);
        }

        private bool RenewLock()
        {
            return this.redisStorage.LockExtend(this.LockKey, this.localLockIdentifier);
        }

        private void ChannelPulse(RedisChannel redisChannel, RedisValue redisValue)
        {
            lock (this.acquireLock)
            {
                Monitor.Pulse(this.acquireLock);
            }
        }

        private void WaitChannelPulse(RedisChannel redisChannel, RedisValue redisValue)
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

        public void Dispose()
        {
            if (this.IsAcquired)
            {
                this.Release();
            }
        }

        private string ApplyPrefix(string prefix)
        {
            return string.Format("{0}{1}", prefix, this.Key);
        }

        private enum PulseType
        {
            One,
            All
        }
    }
}
