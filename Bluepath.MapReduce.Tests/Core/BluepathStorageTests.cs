namespace NetReduce.Core.Tests
{
    using System;
    using System.Linq;

    using Bluepath.MapReduce.Core;
    using Bluepath.Storage.Redis;

    using Shouldly;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class BluepathStorageTests
    {
        private static System.Diagnostics.Process redisProcess;
        private const string Host = "localhost";

        [ClassInitialize]
        public static void FixtureSetup(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext tc)
        {
            // TODO: Exclude test helpers to another project
            redisProcess = Bluepath.Tests.Integration.TestHelpers.SpawnRemoteService(0, Bluepath.Tests.Integration.TestHelpers.ServiceType.Redis);
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
        public void FileSystemStoragCleanMethodDeletesAllFiles()
        {
            using (var redisStorage = new RedisStorage(Host))
            {
                var storage = new BluepathStorage(redisStorage, eraseContents: true);
                storage.Store("a", "aa");
                storage.Store("b", "bb");

                var noOfFiles1 = storage.ListFiles().Count();
                noOfFiles1.ShouldBe(2);

                storage.Clean();
                var noOfFiles2 = storage.ListFiles().Count();

                noOfFiles2.ShouldBe(0);
            }
        }

        [TestMethod]
        public void FileSystemStorageRemoveDeletesGivenFile()
        {
            using (var redisStorage = new RedisStorage(Host))
            {
                var storage = new BluepathStorage(redisStorage, eraseContents: true);
                storage.Store("a", "aa");
                storage.Store("b", "bb");
                var fileToRemove = storage.ListFiles().First(u => u.OriginalString.Contains("a"));

                storage.Remove(fileToRemove);

                var noOfFiles = storage.ListFiles().Count();
                noOfFiles.ShouldBe(1);
                storage.ListFiles().First().OriginalString.ShouldContain("b");
            }
        }
    }
}
