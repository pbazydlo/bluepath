using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Bluepath.Storage;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace Bluepath.Tests.Integration.Storage
{
    [TestClass]
    public class RedisStorageTests
    {
        private static System.Diagnostics.Process redisProcess;

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
        public void RhinoDhtStorageStoresAndRetrievesObjects()
        {
            var objectToStore = "my object";
            var objectName = Guid.NewGuid().ToString();

            using (var storage = new RedisStorage("localhost"))
            {
                storage.Store(objectName, objectToStore);
                string retrievedObject = storage.Retrieve<string>(objectName);

                retrievedObject.ShouldBe(objectToStore);
            }
        }

        [TestMethod]
        public void RhinoDhtStorageDoesntAllowDuplicateStoreWithStoreMethod()
        {
            var objectToStore = "my object";
            var objectName = Guid.NewGuid().ToString();

            using (var storage = new RedisStorage("localhost"))
            {
                storage.Store(objectName, objectToStore);
                Assert.That(() => storage.Store(objectName, objectToStore), Throws.Exception);
            }
        }

        [TestMethod]
        public void RhinoDhtStorageDoesntAllowUpdatingNotExistingObject()
        {
            var objectToStore = "my object";
            var objectName = Guid.NewGuid().ToString();

            using (var storage = new RedisStorage("localhost"))
            {
                Assert.That(() => storage.Update(objectName, objectToStore), Throws.Exception);
            }
        }

        [TestMethod]
        public void RhinoDhtStorageThrowsArgumentOutOfRangeExceptionWhenTryingToGetNotExistingObject()
        {
            var objectName = Guid.NewGuid().ToString();
            using (var storage = new RedisStorage("localhost"))
            {
                Assert.That(() => storage.Retrieve<string>(objectName), Throws.InstanceOf<ArgumentOutOfRangeException>());
            }
        }
    }
}
