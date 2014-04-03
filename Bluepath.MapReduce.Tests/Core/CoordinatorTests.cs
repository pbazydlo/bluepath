namespace NetReduce.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Bluepath.MapReduce;
    using Bluepath.MapReduce.Core;
    using Bluepath.Services;
    using Bluepath.Storage.Redis;
    using Bluepath.Threading;
    using Bluepath.Threading.Schedulers;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Shouldly;

    [TestClass]
    public class CoordinatorTests
    {
        private IMapReduceStorage storage;

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

        [TestInitialize]
        public void Init()
        {
            this.storage = new InMemoryStorage();
        }

        [TestMethod]
        public void CoordinatorWorks()
        {
            using (var redisStorage = new RedisStorage(Host))
            {
                this.storage = new BluepathStorage(redisStorage);
                this.storage.Clean();
                this.storage.Store(Base64.Encode("f1"), "ala ma kota");
                this.storage.Store(Base64.Encode("f2"), "kota alama");
                this.storage.Store(Base64.Encode("f3"), "dolan ma");
                var filesToRead = this.storage.ListFiles();
                var mapperCodeFile = new FileUri("file:///SampleMapper.cs");
                var reducerCodeFile = new FileUri("file:///SampleReducer.cs");
                TestHelpers.LoadToStorage(@"..\..\SampleMapper.cs", mapperCodeFile, this.storage);
                TestHelpers.LoadToStorage(@"..\..\SampleReducer.cs", reducerCodeFile, this.storage);

                var connectionManager = new ConnectionManager(new Dictionary<ServiceUri, PerformanceStatistics>(), null, null, null);
                var scheduler = new ThreadNumberScheduler(connectionManager);
                var coordinator = new Coordinator(connectionManager, scheduler, DistributedThread.ExecutorSelectionMode.LocalOnly);

                coordinator.Start(2, 2, mapperCodeFile, reducerCodeFile, filesToRead.Select(f => new FileUri(f.ToString())));

                string result = string.Empty;
                
                Debug.WriteLine("Listing files...");
                foreach (var file in this.storage.ListFiles())
                {
                    var fileName = this.storage.GetFileName(file);
                    Debug.Write(fileName);
                    try
                    {
                        Debug.WriteLine(" -- {0}", (object)Base64Decode(fileName));
                    }
                    catch
                    {
                        Debug.WriteLine(string.Empty);
                    }
                }

                foreach (var uri in this.storage.ListFiles())
                {
                    var file = this.storage.GetFileName(uri);
                    if (file.Contains("REDUCE") && file.Contains(Base64.Encode("kota")))
                    {
                        result = this.storage.Read(file);
                    }
                }

                result.ShouldBe("2");
            }
        }

        /*[TestMethod]
        public void CoordinatorWorksOnFileSystemStorage()
        {
            var storage = new FileSystemStorage(@"c:\temp\netreduce", eraseContents: true) as IMapReduceStorage;

            storage.Store(Base64.Encode("f1"), "ala ma kota");
            storage.Store(Base64.Encode("f2"), "kota alama");
            storage.Store(Base64.Encode("f3"), "dolan ma");
            var filesToRead = storage.ListFiles();
            var mapperCodeFile = new Uri("file:///SampleMapper.cs");
            var reducerCodeFile = new Uri("file:///SampleReducer.cs");
            TestHelpers.LoadToStorage(@"..\..\SampleMapper.cs", mapperCodeFile, storage);
            TestHelpers.LoadToStorage(@"..\..\SampleReducer.cs", reducerCodeFile, storage);
            var coordinator = new Coordinator<ThreadWorker>(storage);

            coordinator.Start(2, 2, mapperCodeFile, reducerCodeFile, filesToRead);

            string result = string.Empty;
            foreach (var uri in storage.ListFiles())
            {
                var file = this.storage.GetFileName(uri);
                if (file.Contains("REDUCE") && file.Contains(Base64.Encode("kota")))
                {
                    result = storage.Read(file);
                }
            }

            result.ShouldBe("2");
        }*/
        
        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
