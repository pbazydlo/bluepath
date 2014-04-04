using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bluepath.Storage.Redis;
using Bluepath.Storage.Structures.Collections;
using Shouldly;

namespace Bluepath.Tests.Integration.Storage.Structures.Collections
{
    [TestClass]
    public class DistributedListTests
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
        public void DistributedListAllowsAddingAndRemovingItems()
        {
            var storage = new RedisStorage(Host);
            var key = Guid.NewGuid().ToString();
            var list = new DistributedList<int>(storage, key);

            for(int i=0;i<10;i++)
            {
                list.Add(i);
            }

            list.Count.ShouldBe(10);
            list.Remove(5);
            list.Count.ShouldBe(9);
            foreach (var item in list)
            {
                item.ShouldNotBe(5);
            }

        }

        [TestMethod]
        public void DistributedListsWithTheSameKeyShareState()
        {
            var storage = new RedisStorage(Host);
            var key = Guid.NewGuid().ToString();
            var list1 = new DistributedList<int>(storage, key);

            for (int i = 0; i < 10; i++)
            {
                list1.Add(i);
            }

            list1.Count.ShouldBe(10);
            var list2 = new DistributedList<int>(storage, key);
            list2.Count.ShouldBe(10);
        }
    }
}
