using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Bluepath.Services;
using System.Collections.Generic;
using Bluepath.Executor;
using Bluepath.Threading.Schedulers;
using Shouldly;

namespace Bluepath.Tests.DistributedThread.Schedulers
{
    [TestClass]
    public class ThreadNumberSchedulerTests
    {
        [TestMethod]
        public void ThreadNumberSchedulerChoosesServiceWithSmallestNoOfThreads()
        {
            var serviceUri1 = new ServiceUri(){Address="1"};
            var serviceUri2 = new ServiceUri() { Address = "2" };
            var noOfTasks1 = new Dictionary<ExecutorState, int>()
            {
                {ExecutorState.Running, 15}
            };
            var noOfTasks2 = new Dictionary<ExecutorState, int>()
            {
                {ExecutorState.Running, 10}
            };
            Dictionary<ServiceUri, PerformanceStatistics> services
                = new Dictionary<ServiceUri, PerformanceStatistics>()
                {
                    {serviceUri1, new PerformanceStatistics(){NumberOfTasks=noOfTasks1}},
                    {serviceUri2, new PerformanceStatistics(){NumberOfTasks=noOfTasks2}},
                };
            var connectionManagerMock = new Mock<IConnectionManager>(MockBehavior.Strict);
            connectionManagerMock.Setup(cm => cm.RemoteServices).Returns(services);

            var scheduler = new ThreadNumberScheduler(connectionManagerMock.Object);
            scheduler.GetRemoteServiceUri().ShouldBe(serviceUri2);
        }
    }
}
