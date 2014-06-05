namespace Bluepath.Tests.Reporting
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    using Bluepath.Reporting.OpenXes;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class OpenXesTests
    {
        [TestMethod]
        public void OpenXesSerializationTest()
        {
            var case1 = new TraceType("Case1", new[]
                               {
                                  new EventType("Start", "Start", DateTime.Now, EventType.Transition.Start), 
                                  new EventType("Start", "Start", DateTime.Now.AddSeconds(1), EventType.Transition.Complete),
                                  new EventType("Phone Call", "Helen", DateTime.Now.AddSeconds(2), EventType.Transition.Start),
                                  new EventType("Phone Call", "Helen", DateTime.Now.AddSeconds(3), EventType.Transition.Complete),
                                  new EventType("End", "End", DateTime.Now.AddSeconds(4), EventType.Transition.Start),
                                  new EventType("End", "End", DateTime.Now.AddSeconds(5), EventType.Transition.Complete),
                               });
            
            var logType = LogType.Create(new [] { case1 });

            var xml = logType.Serialize();
        }
    }
}
