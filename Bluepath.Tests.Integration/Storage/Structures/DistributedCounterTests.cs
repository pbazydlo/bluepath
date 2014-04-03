using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bluepath.Storage.Structures;
using Bluepath.Storage.Redis;
using Shouldly;

namespace Bluepath.Tests.Integration.Storage.Structures
{
    [TestClass]
    public class DistributedCounterTests
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
        public void DistributedCounterInitialCountIs0()
        {
            using (var storage = new RedisStorage(Host))
            {
                var id = Guid.NewGuid().ToString();
                var counter = new DistributedCounter(storage, id);
                counter.GetValue().ShouldBe(0);
            }
        }

        [TestMethod]
        public void DistributedCounterSharesStateBetweenInstancesWithTheSameId()
        {
            using(var storage = new RedisStorage(Host))
            {
                var id = Guid.NewGuid().ToString();
                var counter1 = new DistributedCounter(storage, id);
                var counter2 = new DistributedCounter(storage, id);
                counter1.SetValue(23);
                counter2.GetValue().ShouldBe(23);
                counter2.Decrease(1);
                counter1.GetValue().ShouldBe(22);
            }
        }
    }
}
