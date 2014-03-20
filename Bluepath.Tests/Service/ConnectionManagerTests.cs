using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bluepath.Services;
using System.Collections.Generic;
using Shouldly;
using Moq;
using Bluepath.Services.Discovery;

namespace Bluepath.Tests.Service
{
    [TestClass]
    public class ConnectionManagerTests
    {
        [TestMethod]
        public void ConnectionManagerHandlesNullListInConstructor()
        {
            List<Bluepath.ServiceReferences.IRemoteExecutorService> services = null;

            var manager = new ConnectionManager(services, null);

            manager.RemoteServices.ShouldBeEmpty();
        }

        [TestMethod]
        public void ConnectionManagerHandlesNullRemoteServiceInConstructor()
        {
            Bluepath.ServiceReferences.IRemoteExecutorService service = null;

            var manager = new ConnectionManager(service, null);

            manager.RemoteServices.ShouldBeEmpty();
        }

        [TestMethod]
        [Timeout(1000)]
        public void ConnectionManagerFetchesServicesFromServiceDiscovery()
        {
            var manualResetEvent = new System.Threading.ManualResetEvent(false);
            var serviceMock1 = new Mock<Bluepath.ServiceReferences.IRemoteExecutorService>(MockBehavior.Strict);
            var serviceMock2 = new Mock<Bluepath.ServiceReferences.IRemoteExecutorService>(MockBehavior.Strict);
            List<Bluepath.ServiceReferences.IRemoteExecutorService> services
                = new List<ServiceReferences.IRemoteExecutorService>()
                {
                    serviceMock1.Object,
                    serviceMock2.Object
                };
            var serviceDiscoveryMock = new Mock<IServiceDiscovery>(MockBehavior.Strict);
            serviceDiscoveryMock.Setup(sd => sd.GetAvailableServices()).Returns(services).Callback(() => manualResetEvent.Set());

            var manager = new ConnectionManager(remoteService: null, listener: null,
                serviceDiscovery: serviceDiscoveryMock.Object);

            manualResetEvent.WaitOne();

            manager.RemoteServices.Count().ShouldBe(2);
        }

        [TestMethod]
        [Timeout(1000)]
        public void ConnectionManagerAddsNewServicesFromServiceDiscovery()
        {
            var manualResetEvent = new System.Threading.ManualResetEvent(false);
            var serviceMock1 = new Mock<Bluepath.ServiceReferences.IRemoteExecutorService>(MockBehavior.Strict);
            var serviceMock2 = new Mock<Bluepath.ServiceReferences.IRemoteExecutorService>(MockBehavior.Strict);
            var serviceMock3 = new Mock<Bluepath.ServiceReferences.IRemoteExecutorService>(MockBehavior.Strict);
            List<Bluepath.ServiceReferences.IRemoteExecutorService> services
                = new List<ServiceReferences.IRemoteExecutorService>()
                {
                    serviceMock1.Object,
                    serviceMock2.Object
                };
            var serviceDiscoveryMock = new Mock<IServiceDiscovery>(MockBehavior.Strict);
            serviceDiscoveryMock.Setup(sd => sd.GetAvailableServices()).Returns(() => services).Callback(() => manualResetEvent.Set());

            var manager = new ConnectionManager(remoteService: null, listener: null,
                serviceDiscovery: serviceDiscoveryMock.Object, 
                serviceDiscoveryPeriod: new TimeSpan(days: 0,hours: 0, minutes:0, seconds:0, milliseconds: 10));

            manualResetEvent.WaitOne();
            manager.RemoteServices.Count().ShouldBe(2);
            services.Add(serviceMock3.Object);
            manualResetEvent.Reset();
            manualResetEvent.WaitOne();

            manager.RemoteServices.Count().ShouldBe(3);
        }

        [TestMethod]
        [Timeout(1000)]
        public void ConnectionManagerRemovesServicesNotExistingInServiceDiscovery()
        {
            var manualResetEvent = new System.Threading.ManualResetEvent(false);
            var serviceMock1 = new Mock<Bluepath.ServiceReferences.IRemoteExecutorService>(MockBehavior.Strict);
            var serviceMock2 = new Mock<Bluepath.ServiceReferences.IRemoteExecutorService>(MockBehavior.Strict);
            List<Bluepath.ServiceReferences.IRemoteExecutorService> services
                = new List<ServiceReferences.IRemoteExecutorService>()
                {
                    serviceMock1.Object,
                    serviceMock2.Object
                };
            var serviceDiscoveryMock = new Mock<IServiceDiscovery>(MockBehavior.Strict);
            serviceDiscoveryMock.Setup(sd => sd.GetAvailableServices()).Returns(() => services).Callback(() => manualResetEvent.Set());

            var manager = new ConnectionManager(remoteService: null, listener: null,
                serviceDiscovery: serviceDiscoveryMock.Object,
                serviceDiscoveryPeriod: new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 10));

            manualResetEvent.WaitOne();
            System.Threading.Thread.Sleep(10);
            manager.RemoteServices.Count().ShouldBe(2);
            services.RemoveAt(1);
            manualResetEvent.Reset();
            manualResetEvent.WaitOne();

            manager.RemoteServices.Count().ShouldBe(1);
        }
    }
}
