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

        [ClassCleanup]
        public static void FixtureTearDown()
        {
            if (redisProcess != null)
            {
                redisProcess.Kill();
            }
        }

        [TestMethod]
        public void DistributedDictionaryAllowsAddingAndReadingEntries()
        {
            var storage = new RedisStorage(Host);
            var dictionary = new Bluepath.Storage.Structures.Collections.DistributedDictionary<int, string>();
            dictionary.Add(0, "ala");
            dictionary.Add(2, "ola");
            dictionary.Add(18, "zuza");

            dictionary[18].ShouldBe("zuza");
        }

        [TestMethod]
        public void DistributedDictionaryProperlyManagesKeysCollection()
        {
            var storage = new RedisStorage(Host);
            var dictionary = new Bluepath.Storage.Structures.Collections.DistributedDictionary<int, string>();
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
            var dictionary = new Bluepath.Storage.Structures.Collections.DistributedDictionary<int, string>();
            dictionary.Add(0, "ala");
            dictionary.Add(2, "ola");
            dictionary.Add(18, "zuza");

            dictionary.Values.Count.ShouldBe(3);
            dictionary.Values.ShouldContain("ola");
        }
    }
}
