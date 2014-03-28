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
        public void ConnectionManagerHandlesNullDictionaryInConstructor()
        {
            Dictionary<ServiceUri, PerformanceStatistics> services = null;

            var manager = new ConnectionManager(services, null);

            manager.RemoteServices.ShouldBeEmpty();
        }

        [TestMethod]
        public void ConnectionManagerHandlesNullRemoteServiceInConstructor()
        {
            KeyValuePair<ServiceUri, PerformanceStatistics>? service = null;

            var manager = new ConnectionManager(service, null);

            manager.RemoteServices.ShouldBeEmpty();
        }

        [TestMethod]
        [Timeout(1000)]
        public void ConnectionManagerFetchesServicesFromServiceDiscovery()
        {
            var manualResetEvent = new System.Threading.ManualResetEvent(false);
            var serviceUri1 = new ServiceUri() { Address = "1" };
            var serviceUri2 = new ServiceUri() { Address = "2" };
            Dictionary<ServiceUri, PerformanceStatistics> services
                = new Dictionary<ServiceUri, PerformanceStatistics>()
                {
                    {serviceUri1, new PerformanceStatistics()},
                    {serviceUri2, new PerformanceStatistics()}
                };
            var serviceDiscoveryMock = new Mock<IServiceDiscovery>(MockBehavior.Strict);
            serviceDiscoveryMock.Setup(sd => sd.GetPerformanceStatistics()).Returns(services).Callback(() => manualResetEvent.Set());

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
            var serviceUri1 = new ServiceUri() { Address = "1" };
            var serviceUri2 = new ServiceUri() { Address = "2" };
            Dictionary<ServiceUri, PerformanceStatistics> services
                = new Dictionary<ServiceUri, PerformanceStatistics>()
                {
                    {serviceUri1, new PerformanceStatistics()},
                    {serviceUri2, new PerformanceStatistics()}
                };
            var serviceDiscoveryMock = new Mock<IServiceDiscovery>(MockBehavior.Strict);
            serviceDiscoveryMock.Setup(sd => sd.GetPerformanceStatistics()).Returns(() => services).Callback(() => manualResetEvent.Set());

            var manager = new ConnectionManager(remoteService: null, listener: null,
                serviceDiscovery: serviceDiscoveryMock.Object,
                serviceDiscoveryPeriod: new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 10));

            manualResetEvent.WaitOne();
            manager.RemoteServices.Count().ShouldBe(2);
            services.Add(new ServiceUri(), new PerformanceStatistics());
            manualResetEvent.Reset();
            manualResetEvent.WaitOne();

            manager.RemoteServices.Count().ShouldBe(3);
        }

        [TestMethod]
        [Timeout(1000)]
        public void ConnectionManagerRemovesServicesNotExistingInServiceDiscovery()
        {
            var manualResetEvent = new System.Threading.ManualResetEvent(false);
            var serviceUri1 = new ServiceUri()
            {
                Address = "jack",
                BindingType = BindingType.BasicHttpBinding
            };

            var serviceUri2 = new ServiceUri()
            {
                Address = "jackie",
                BindingType = BindingType.BasicHttpBinding
            };

            Dictionary<ServiceUri, PerformanceStatistics> services
                = new Dictionary<ServiceUri, PerformanceStatistics>()
                {
                    {serviceUri1, new PerformanceStatistics()},
                    {serviceUri2, new PerformanceStatistics()}
                };
            var serviceDiscoveryMock = new Mock<IServiceDiscovery>(MockBehavior.Strict);
            serviceDiscoveryMock.Setup(sd => sd.GetPerformanceStatistics()).Returns(() => services).Callback(() => manualResetEvent.Set());

            var manager = new ConnectionManager(remoteService: null, listener: null,
                serviceDiscovery: serviceDiscoveryMock.Object,
                serviceDiscoveryPeriod: new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 10));

            manualResetEvent.WaitOne();
            System.Threading.Thread.Sleep(10);
            manager.RemoteServices.Count().ShouldBe(2);
            services.Remove(serviceUri1);
            manualResetEvent.Reset();
            manualResetEvent.WaitOne();

            manager.RemoteServices.Count().ShouldBe(1);
        }
    }
}
