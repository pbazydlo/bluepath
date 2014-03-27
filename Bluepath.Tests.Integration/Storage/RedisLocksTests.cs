using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bluepath.Storage.Redis;
using Assert = NUnit.Framework.Assert;
using Throws = NUnit.Framework.Throws;
using Shouldly;
using Bluepath.Storage.Locks;

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

        [ClassCleanup]
        public static void FixtureTearDown()
        {
            if (redisProcess != null)
            {
                redisProcess.Kill();
            }
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
    }
}
