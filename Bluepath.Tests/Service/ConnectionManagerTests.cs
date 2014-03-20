using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bluepath.Services;
using System.Collections.Generic;
using Shouldly;

namespace Bluepath.Tests.Service
{
    [TestClass]
    public class ConnectionManagerTests
    {
        private const string Category = "ConnectionManager";

        [TestMethod]
        [TestCategory(Category)]
        public void ConnectionManagerHandlesNullListInConstructor()
        {
            List<Bluepath.ServiceReferences.IRemoteExecutorService> services = null;

            var manager = new ConnectionManager(services, null);

            manager.RemoteServices.ShouldBeEmpty();
        }

        [TestMethod]
        [TestCategory(Category)]
        public void ConnectionManagerHandlesNullRemoteServiceInConstructor()
        {
            Bluepath.ServiceReferences.IRemoteExecutorService service = null;

            var manager = new ConnectionManager(service, null);

            manager.RemoteServices.ShouldBeEmpty();
        }
       
    }
}
