using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using Bluepath.Tests.Integration.DistributedThread;
using Bluepath.Storage.Redis;
using System.Collections.Generic;

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

        [TestMethod]
        public void RedisStorageStoresAndRetrievesObjects()
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
        public void RedisStorageStoresAndRetrievesComplexObjects()
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
        public void RedisStorageStoresAndRemovesObjects()
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
        public void RedisStorageDoesntAllowDuplicateStoreWithStoreMethod()
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
        public void RedisStorageDoesntAllowUpdatingNotExistingObject()
        {
            var objectToStore = "my object";
            var objectName = Guid.NewGuid().ToString();

            using (var storage = new RedisStorage(RedisStorageTests.Host))
            {
                Assert.That(() => storage.Update(objectName, objectToStore), Throws.Exception);
            }
        }

        [TestMethod]
        public void RedisStorageThrowsArgumentOutOfRangeExceptionWhenTryingToGetNotExistingObject()
        {
            var objectName = Guid.NewGuid().ToString();
            using (var storage = new RedisStorage(RedisStorageTests.Host))
            {
                Assert.That(() => storage.Retrieve<string>(objectName), Throws.InstanceOf<ArgumentOutOfRangeException>());
            }
        }

        [TestMethod]
        public void RedisStorageStoresAndRemovesObjectsInBulks()
        {
            KeyValuePair<string, string>[] objectsToStore = new KeyValuePair<string, string>[100];
            for (int i = 0; i < objectsToStore.Length; i++)
            {
                objectsToStore[i] = new KeyValuePair<string, string>(Guid.NewGuid().ToString(), "jack checked chicken");
            }

            using (var storage = new RedisStorage(RedisStorageTests.Host))
            {
                storage.BulkStore(objectsToStore);
                storage.BulkRemove(objectsToStore.Select(o => o.Key).ToArray());
                foreach (var @object in objectsToStore)
                {
                    Assert.That(() => storage.Retrieve<string>(@object.Key),
                        Throws.InstanceOf<ArgumentOutOfRangeException>());
                }
            }
        }

        [TestMethod]
        public void RedisStorageStoresAndRetrievesComplexObjectsInBulks()
        {
            var objectToStore = new ComplexParameter()
            {
                SomeProperty = "this is string",
                AnotherProperty = 47
            };
            KeyValuePair<string, ComplexParameter>[] objectsToStore = new KeyValuePair<string, ComplexParameter>[100];
            for (int i = 0; i < objectsToStore.Length; i++)
            {
                objectsToStore[i] = new KeyValuePair<string, ComplexParameter>(Guid.NewGuid().ToString(), objectToStore);
            }

            using (var storage = new RedisStorage(RedisStorageTests.Host))
            {
                storage.BulkStore(objectsToStore);
                var retrievedObjects = storage.BulkRetrieve<ComplexParameter>(objectsToStore.Select(o => o.Key).ToArray());

                foreach (var retrievedObject in retrievedObjects)
                {
                    retrievedObject.SomeProperty.ShouldBe(objectToStore.SomeProperty);
                    retrievedObject.AnotherProperty.ShouldBe(objectToStore.AnotherProperty);
                }
            }
        }
    }
}
