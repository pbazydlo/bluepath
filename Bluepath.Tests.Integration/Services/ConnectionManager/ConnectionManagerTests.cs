using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Bluepath.Tests.Integration.Services.ConnectionManager
{
    [TestClass]
    public class ConnectionManagerTests
    {
        [TestMethod]
        public void ConnectionManagerFetchesServicesFromRemoteServiceDiscovery()
        {
            var serviceDiscoveryHost = new CentralizedDiscovery.CentralizedDiscoveryListener("localhost", 20000);
            var bluepathListener1 = new Bluepath.Services.BluepathListener("localhost", 20001);
            var bluepathListener2 = new Bluepath.Services.BluepathListener("localhost", 20002);
            try
            {
                using(var serviceDiscoveryClient1
                    = new CentralizedDiscovery.Client.CentralizedDiscovery(serviceDiscoveryHost.MasterUri, bluepathListener1))
                {
                    using(var serviceDiscoveryClient2
                        = new CentralizedDiscovery.Client.CentralizedDiscovery(serviceDiscoveryHost.MasterUri, bluepathListener2))
                    {
                        var connectionManager = new Bluepath.Services.ConnectionManager(remoteService: null,
                            listener: bluepathListener1,
                            serviceDiscovery: serviceDiscoveryClient1);
                        System.Threading.Thread.Sleep(1000);

                        connectionManager.RemoteServices.Count().ShouldBe(1);
                    }
                }
            }
            finally
            {
                serviceDiscoveryHost.Stop();
                bluepathListener1.Stop();
                bluepathListener2.Stop();
            }
        }
    }
}
