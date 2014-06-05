namespace Bluepath.Tests.Reporting
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Linq;

    using Shouldly;

    using Bluepath.Reporting.OpenXes;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Bluepath.Storage.Structures.Collections;
    using Bluepath.Storage.Redis;
    using Bluepath.Tests.Integration;

    [TestClass]
    public class OpenXesTests
    {
        private static System.Diagnostics.Process redisProcess;
        private const string Host = "localhost";

        [ClassInitialize]
        public static void FixtureSetup(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext tc)
        {
            redisProcess = TestHelpers.SpawnRemoteService(0, TestHelpers.ServiceType.Redis);
        }

        [TestMethod]
        public void OpenXesSerializationTest()
        {
            var storage = new RedisStorage(Host);
            var key = Guid.NewGuid().ToString();
            var list = new DistributedList<EventType>(storage, key);

            list.Add(new EventType("Start", "Start", DateTime.Now, EventType.Transition.Start)); 
            list.Add(new EventType("Start", "Start", DateTime.Now.AddSeconds(1), EventType.Transition.Complete));
            list.Add(new EventType("Phone Call", "Helen", DateTime.Now.AddSeconds(2), EventType.Transition.Start));
            list.Add(new EventType("Phone Call", "Helen", DateTime.Now.AddSeconds(3), EventType.Transition.Complete));
            list.Add(new EventType("End", "End", DateTime.Now.AddSeconds(4), EventType.Transition.Start));
            list.Add(new EventType("End", "End", DateTime.Now.AddSeconds(5), EventType.Transition.Complete));

            storage = new RedisStorage(Host);
            list = new DistributedList<EventType>(storage, key);

            var localList = list.ToList();
            localList.Count.ShouldBe(6);
            localList.Where(e => e != null).Count().ShouldBe(6);

            var @case = new TraceType(Guid.NewGuid().ToString(), list);
            var log = LogType.Create(new[] { @case });
            var xml = log.Serialize();

            Assert.IsTrue(xml.Contains("Phone Call"));
        }
    }
}
