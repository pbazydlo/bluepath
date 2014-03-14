namespace Bluepath.Tests.Integration.Storage
{
    using System;

    using Bluepath.Storage;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NUnit.Framework;

    using Shouldly;

    using Assert = NUnit.Framework.Assert;
    using System.Collections.Generic;

    [TestClass]
    public class RhinoDhtStorageTests
    {
        private static object singleTestLock = new object();
        
        [TestInitialize]
        public void Init()
        {
            System.Threading.Monitor.Enter(singleTestLock);
            List<string> directories = new List<string>()
            {
                "master.esent",
                "node.data.esent",
                "node.queue.esent"
            };

            foreach (var directory in directories)
            {
                if (System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.Delete(directory, true);
                }
            }
        }

        [TestCleanup]
        public void CleanUp()
        {
            System.Threading.Monitor.Exit(singleTestLock);
        }

        [TestMethod]
        public void RhinoDhtStorageStoresAndRetrievesObjects()
        {
            var objectToStore = "my object";
            var objectName = "name";
            using (var storage = new RhinoDhtStorage())
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
            var objectName = "name";
            using (var storage = new RhinoDhtStorage())
            {
                storage.Store(objectName, objectToStore);
                Assert.That(() => storage.Store(objectName, objectToStore), Throws.Exception);
            }
        }

        [TestMethod]
        public void RhinoDhtStorageDoesntAllowUpdatingNotExistingObject()
        {
            var objectToStore = "my object";
            var objectName = "name";
            using (var storage = new RhinoDhtStorage())
            {
                Assert.That(() => storage.Update(objectName, objectToStore), Throws.Exception);
            }
        }

        [TestMethod]
        public void RhinoDhtStorageThrowsArgumentOutOfRangeExceptionWhenTryingToGetNotExistingObject()
        {
            var objectName = "name2";
            using (var storage = new RhinoDhtStorage())
            {
                Assert.That(() => storage.Retrieve<string>(objectName), Throws.InstanceOf<ArgumentOutOfRangeException>());
            }
        }
    }
}
