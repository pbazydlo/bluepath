using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bluepath.Storage.Redis;
using Shouldly;

namespace Bluepath.Tests.Integration.Storage.Structures.Collections
{
    [TestClass]
    public class DistributedDictionaryTests
    {
        private static System.Diagnostics.Process redisProcess;
        private const string Host = "localhost";

        [ClassInitialize]
        public static void FixtureSetup(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext tc)
        {
            redisProcess = TestHelpers.SpawnRemoteService(0, TestHelpers.ServiceType.Redis);
        }

        [TestMethod]
        public void DistributedDictionaryAllowsAddingAndReadingEntries()
        {
            var storage = new RedisStorage(Host);
            var id = Guid.NewGuid().ToString();
            var dictionary = new Bluepath.Storage.Structures.Collections.DistributedDictionary<int, string>(storage, id);
            dictionary.Add(0, "ala");
            dictionary.Add(2, "ola");
            dictionary.Add(18, "zuza");

            dictionary[18].ShouldBe("zuza");
        }

        [TestMethod]
        public void DistributedDictionaryAllowsRemovingEntries()
        {
            var storage = new RedisStorage(Host);
            var id = Guid.NewGuid().ToString();
            var dictionary = new Bluepath.Storage.Structures.Collections.DistributedDictionary<int, string>(storage, id);
            dictionary.Add(0, "ala");
            dictionary.Add(2, "ola");
            dictionary.Add(18, "zuza");

            dictionary.ContainsKey(2).ShouldBe(true);

            dictionary.Remove(2);

            dictionary.ContainsKey(2).ShouldBe(false);
        }

        [TestMethod]
        public void DistributedDictionaryProperlyManagesKeysCollection()
        {
            var storage = new RedisStorage(Host);
            var id = Guid.NewGuid().ToString();
            var dictionary = new Bluepath.Storage.Structures.Collections.DistributedDictionary<int, string>(storage, id);
            dictionary.Add(0, "ala");
            dictionary.Add(2, "ola");
            dictionary.Add(18, "zuza");

            dictionary.ContainsKey(2).ShouldBe(true);
            dictionary.Keys.Count.ShouldBe(3);
        }

        [TestMethod]
        public void DistributedDictionaryProperlyManagesValuesCollection()
        {
            var storage = new RedisStorage(Host);
            var id = Guid.NewGuid().ToString();
            var dictionary = new Bluepath.Storage.Structures.Collections.DistributedDictionary<int, string>(storage, id);
            dictionary.Add(0, "ala");
            dictionary.Add(2, "ola");
            dictionary.Add(18, "zuza");

            dictionary.Values.Count.ShouldBe(3);
            dictionary.Values.ShouldContain("ola");
        }

        [TestMethod]
        public void DistributedDictionaryHandlesDistributedListAsValue()
        {
            var storage = new RedisStorage(Host);
            var id = Guid.NewGuid().ToString();
            var dictionary = new Bluepath.Storage.Structures.Collections.DistributedDictionary<int, Bluepath.Storage.Structures.Collections.DistributedList<string>>(storage, id);
            var distributedList1 = new Bluepath.Storage.Structures.Collections.DistributedList<string>(storage, Guid.NewGuid().ToString());
            var distributedList2 = new Bluepath.Storage.Structures.Collections.DistributedList<string>(storage, Guid.NewGuid().ToString());
            distributedList1.Add("jack");
            distributedList1.Add("checked");
            distributedList1.Add("chicken");

            distributedList2.Add("in");
            distributedList2.Add("the");

            dictionary.Add(0, distributedList1);
            dictionary.Add(1, distributedList2);

            var dictionaryCheck = new Bluepath.Storage.Structures.Collections.DistributedDictionary<int, Bluepath.Storage.Structures.Collections.DistributedList<string>>(storage, id);
            dictionaryCheck.Count.ShouldBe(2);
            var checkList1 = dictionaryCheck[0];
            //checkList1.Storage = storage;
            var checkList2 = dictionaryCheck[1];
            //checkList2.Storage = storage;
            checkList1.Count.ShouldBe(3);
            checkList2.Count.ShouldBe(2);
            checkList1[0].ShouldBe("jack");
        }
    }
}
