using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Moq;
using Bluepath.Storage;
using Bluepath.Storage.Locks;

namespace Bluepath.Tests.Reporting
{
    [TestClass]
    public class LogTests
    {
        [TestMethod]
        public void AssureTimeMonotonicityWorks()
        {
            var storageMock = new Mock<IExtendedStorage>(MockBehavior.Strict);
            var disposableMock = new Mock<IStorageLock>();
            DateTime? storedValue = null;
            storageMock.Setup(st => st.AcquireLock(It.IsAny<string>())).Returns(disposableMock.Object);
            storageMock.Setup(st => st.StoreOrUpdate(It.IsAny<string>(), It.IsAny<DateTime>())).Callback<string, DateTime>((key, date) =>
            {
                storedValue = date;
            });
            storageMock.Setup(st => st.Retrieve<DateTime>(It.IsAny<string>())).Returns<string>((key) =>
                {
                    return storedValue.Value;
                });
            var startingDateTime = new DateTime(1990,12,12);
            var storageKey = "jack";
            
            var result = Log.AssureTimeMonotonicity(startingDateTime, storageMock.Object, storageKey);
            var result1 = Log.AssureTimeMonotonicity(startingDateTime, storageMock.Object, storageKey);

            result.ShouldBe(startingDateTime);
            result1.ShouldBeGreaterThan(startingDateTime);
        }
    }
}
