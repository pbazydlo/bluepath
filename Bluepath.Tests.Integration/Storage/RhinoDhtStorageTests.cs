namespace Bluepath.Tests.Integration.Storage
{
    using System;

    using Bluepath.Storage;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using NUnit.Framework;

    using Shouldly;

    using Assert = NUnit.Framework.Assert;

    [TestClass]
    public class RhinoDhtStorageTests
    {
        [TestMethod]
        public void RhinoDhtStorageStoresAndRetrievesObjects()
        {
            var objectToStore = "my object";
            var objectName = "name";
            var storage = new RhinoDhtStorage();
            
            storage.Store(objectName, objectToStore);
            string retrievedObject = storage.Retrieve<string>(objectName);

            retrievedObject.ShouldBe(objectToStore);
        }

        [TestMethod]
        public void RhinoDhtStorageDoesntAllowDuplicateStoreWithStoreMethod()
        {
            var objectToStore = "my object";
            var objectName = "name";
            var storage = new RhinoDhtStorage();

            storage.Store(objectName, objectToStore);
            Assert.That(() => storage.Store(objectName, objectToStore), Throws.Exception);
        }

        [TestMethod]
        public void RhinoDhtStorageDoesntAllowUpdatingNotExistingObject()
        {
            var objectToStore = "my object";
            var objectName = "name";
            var storage = new RhinoDhtStorage();

            Assert.That(() => storage.Update(objectName, objectToStore), Throws.Exception);
        }

        [TestMethod]
        public void RhinoDhtStorageThrowsArgumentOutOfRangeExceptionWhenTryingToGetNotExistingObject()
        {
            var objectName = "name2";
            var storage = new RhinoDhtStorage();

            Assert.That(() => storage.Retrieve<string>(objectName), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
