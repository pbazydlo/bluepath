using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bluepath.Storage.Redis;
using Assert = NUnit.Framework.Assert;
using Throws = NUnit.Framework.Throws;
using Shouldly;
using Bluepath.Storage.Locks;
using System.Threading;

namespace Bluepath.Tests.Integration.Storage
{
    [TestClass]
    public class RedisLocksTests
    {
        private static System.Diagnostics.Process redisProcess;
        private const string Host = "localhost";

        [ClassInitialize]
        public static void FixtureSetup(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext tc)
        {
            redisProcess = TestHelpers.SpawnRemoteService(0, TestHelpers.ServiceType.Redis);
        }

        [TestMethod]
        public void RedisLockCannotBeAcquiredTwice()
        {
            using(var storage = new RedisStorage(Host))
            {
                var lockKey = Guid.NewGuid().ToString();
                using(var @lock = storage.AcquireLock(lockKey))
                {
                    Assert.That(() => @lock.Acquire(), Throws.Exception);
                }
            }
        }

        [TestMethod]
        public void RedisLockCannotBeAcquiredWhenKeyIsLocked()
        {
            using(var storage = new RedisStorage(Host))
            {
                var lockKey = Guid.NewGuid().ToString();
                using(var @lock = storage.AcquireLock(lockKey))
                {
                    bool isAnotherLockAcquired = false;
                    var acquireThread = new System.Threading.Thread(() =>
                    {
                        using (var anotherLock = storage.AcquireLock(lockKey))
                        {
                            isAnotherLockAcquired = true;
                        }
                    });

                    acquireThread.Start();
                    System.Threading.Thread.Sleep(100);

                    isAnotherLockAcquired.ShouldBe(false);
                }
            }
        }

        [TestMethod]
        public void RedisLockIsProperlyReleased()
        {
            using (var storage = new RedisStorage(Host))
            {
                var lockKey = Guid.NewGuid().ToString();
                using (var @lock = storage.AcquireLock(lockKey))
                {
                    @lock.IsAcquired.ShouldBe(true);
                }

                using (var @lock = storage.AcquireLock(lockKey))
                {
                    @lock.IsAcquired.ShouldBe(true);
                }
            }
        }

        [TestMethod]
        public void RedisLockAllowsAcquireWithTimeout()
        {
            using (var storage = new RedisStorage(Host))
            {
                var lockKey = Guid.NewGuid().ToString();
                using (var @lock = storage.AcquireLock(lockKey))
                {
                    IStorageLock anotherLock;
                    var acquireResult = 
                        storage.AcquireLock(lockKey, TimeSpan.FromMilliseconds(10), out anotherLock);
                    acquireResult.ShouldBe(false);
                    anotherLock.IsAcquired.ShouldBe(false);
                }
            }
        }

        [TestMethod]
        public void RedisLockAllowsAcquireAfterReleaseTimeout()
        {
            using (var storage = new RedisStorage(Host))
            {
                var lockKey = Guid.NewGuid().ToString();
                var @lock = storage.AcquireLock(lockKey);
                @lock.IsAcquired.ShouldBe(true);
                @lock.Release();
                @lock.IsAcquired.ShouldBe(false);
                @lock.Acquire();
                @lock.IsAcquired.ShouldBe(true);
                @lock.Dispose();
            }
        }

        [TestMethod]
        public void RedisLockAllowsWaitingAndWakingALock()
        {
            using (var storage = new RedisStorage(Host))
            {
                var lockKey = Guid.NewGuid().ToString();
                bool isWaiting = false;
                bool isFinished = false;
                var waitingThread = new Thread(() =>
                {
                    using(var @lock = storage.AcquireLock(lockKey))
                    {
                        isWaiting = true;
                        @lock.Wait();
                    }

                    isFinished = true;
                });
                waitingThread.Start();
                TestHelpers.RepeatUntilTrue(() => isWaiting, times: 5);
                isWaiting.ShouldBe(true);
                isFinished.ShouldBe(false);
                using(var anotherLock = storage.AcquireLock(lockKey))
                {
                    anotherLock.PulseAll();
                }

                TestHelpers.RepeatUntilTrue(() => isFinished, times: 5);
                isFinished.ShouldBe(true);
            }
        }

        [TestMethod]
        public void RedisLockAllowsWaitingOnALockWithTimeout()
        {
            using (var storage = new RedisStorage(Host))
            {
                var lockKey = Guid.NewGuid().ToString();
                bool isWaiting = false;
                bool isFinished = false;
                var waitingThread = new Thread(() =>
                {
                    using (var @lock = storage.AcquireLock(lockKey))
                    {
                        isWaiting = true;
                        @lock.Wait(TimeSpan.FromMilliseconds(100));
                    }

                    isFinished = true;
                });
                waitingThread.Start();
                Thread.Sleep(200);
                isWaiting.ShouldBe(true);
                isFinished.ShouldBe(true);
            }
        }

        [TestMethod]
        public void RedisLockPulseWakesSingleThread()
        {
            using (var storage = new RedisStorage(Host))
            {
                var lockKey = Guid.NewGuid().ToString();
                bool isWaiting1 = false;
                bool isFinished1 = false;
                var waitingThread1 = new Thread(() =>
                {
                    using (var @lock = storage.AcquireLock(lockKey))
                    {
                        isWaiting1 = true;
                        @lock.Wait();
                    }

                    isFinished1 = true;
                });
                bool isWaiting2 = false;
                bool isFinished2 = false;
                var waitingThread2 = new Thread(() =>
                {
                    using (var @lock = storage.AcquireLock(lockKey))
                    {
                        isWaiting2 = true;
                        @lock.Wait();
                    }

                    isFinished2 = true;
                });
                waitingThread1.Start();
                waitingThread2.Start();
                TestHelpers.RepeatUntilTrue(() => isWaiting1 && isWaiting2, times: 5);
                isWaiting1.ShouldBe(true);
                isWaiting2.ShouldBe(true);
                isFinished1.ShouldBe(false);
                isFinished2.ShouldBe(false);
                using (var anotherLock = storage.AcquireLock(lockKey))
                {
                    anotherLock.Pulse();
                }

                TestHelpers.RepeatUntilTrue(() => ((isFinished1 || isFinished2) && !(isFinished1 && isFinished2)), times: 5);
                ((isFinished1 || isFinished2) && !(isFinished1 && isFinished2)).ShouldBe(true);
                using (var anotherLock = storage.AcquireLock(lockKey))
                {
                    anotherLock.Pulse();
                }

                TestHelpers.RepeatUntilTrue(() => isFinished1 && isFinished2, times: 5);
                isFinished1.ShouldBe(true);
                isFinished2.ShouldBe(true);
            }
        }
    }
}
