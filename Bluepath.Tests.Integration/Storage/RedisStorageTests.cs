using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Bluepath.Tests.Integration.DistributedThread;
using Bluepath.Storage.Redis;

namespace Bluepath.Tests.Integration.Storage
{
    [TestClass]
    public class RedisStorageTests
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
        public void RhinoDhtStorageStoresAndRetrievesObjects()
        {
            var objectToStore = "my object";
            var objectName = Guid.NewGuid().ToString();

            using (var storage = new RedisStorage(RedisStorageTests.Host))
            {
                storage.Store(objectName, objectToStore);
                string retrievedObject = storage.Retrieve<string>(objectName);

                retrievedObject.ShouldBe(objectToStore);
            }
        }

        [TestMethod]
        public void RhinoDhtStorageStoresAndRetrievesComplexObjects()
        {
            var objectToStore = new ComplexParameter()
                {
                    SomeProperty = "this is string",
                    AnotherProperty = 47
                };
            var objectName = Guid.NewGuid().ToString();

            using (var storage = new RedisStorage(RedisStorageTests.Host))
            {
                storage.Store(objectName, objectToStore);
                var retrievedObject = storage.Retrieve<ComplexParameter>(objectName);

                retrievedObject.SomeProperty.ShouldBe(objectToStore.SomeProperty);
                retrievedObject.AnotherProperty.ShouldBe(objectToStore.AnotherProperty);
            }
        }

        [TestMethod]
        public void RhinoDhtStorageStoresAndRemovesObjects()
        {
            var objectToStore = "my object";
            var objectName = Guid.NewGuid().ToString();

            using (var storage = new RedisStorage(RedisStorageTests.Host))
            {
                storage.Store(objectName, objectToStore);
                storage.Remove(objectName);
                Assert.That(() => storage.Retrieve<string>(objectName), 
                    Throws.InstanceOf<ArgumentOutOfRangeException>());
            }
        }

        [TestMethod]
        public void RhinoDhtStorageDoesntAllowDuplicateStoreWithStoreMethod()
        {
            var objectToStore = "my object";
            var objectName = Guid.NewGuid().ToString();

            using (var storage = new RedisStorage(RedisStorageTests.Host))
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

            using (var storage = new RedisStorage(RedisStorageTests.Host))
            {
                Assert.That(() => storage.Update(objectName, objectToStore), Throws.Exception);
            }
        }

        [TestMethod]
        public void RhinoDhtStorageThrowsArgumentOutOfRangeExceptionWhenTryingToGetNotExistingObject()
        {
            var objectName = Guid.NewGuid().ToString();
            using (var storage = new RedisStorage(RedisStorageTests.Host))
            {
                Assert.That(() => storage.Retrieve<string>(objectName), Throws.InstanceOf<ArgumentOutOfRangeException>());
            }
        }
    }
}
