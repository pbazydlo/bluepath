namespace Bluepath.Tests.DistributedThread.Schedulers
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Bluepath.Services;
    using Bluepath.Threading.Schedulers;
    using Shouldly;

    [TestClass]
    public class RoundRobinLocalSchedulerTests
    {
        [TestMethod]
        public void RoundRobinLocalSchedulerGoesSequentiallyThroughAllSpecifiedServiceUris()
        {
            int noOfUris = 10;
            var serviceUris = new ServiceUri[noOfUris];
            for(int i=0;i<noOfUris;i++)
            {
                serviceUris[i] = new ServiceUri() { Address = i.ToString() };
            }

            var scheduler = new RoundRobinLocalScheduler(serviceUris);
            for(int i=0;i<noOfUris;i++)
            {
                scheduler.GetRemoteServiceUri().Address.ShouldBe(serviceUris[i].Address);
            }

            scheduler.GetRemoteServiceUri().Address.ShouldBe(serviceUris[0].Address);
        }
    }
}
