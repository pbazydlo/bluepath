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
                        this.RepeatUntilTrue(() => connectionManager.RemoteServices.Count() == 1, times: 10);

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

        private void RepeatUntilTrue(Func<bool> function, int times = 5, TimeSpan? wait = null)
        {
            var waitTime = wait ?? new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 500);
            int timesExecuted = 0;
            while(timesExecuted< times && !function())
            {
                System.Threading.Thread.Sleep(waitTime);
            }
        }
    }
}
