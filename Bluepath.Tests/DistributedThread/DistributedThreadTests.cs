namespace Bluepath.Tests.DistributedThread
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Bluepath.Services;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Shouldly;

    using IRemoteExecutorService = Bluepath.ServiceReferences.IRemoteExecutorService;
    using Bluepath.Threading.Schedulers;

    [TestClass]
    public class DistributedThreadTests
    {
        [TestMethod]
        public void ExecutesSingleThread()
        {
            var listToProcess = new List<int>()
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9
            };

            Func<List<int>, int, int, int, int?> function = (list, start, stop, threshold) =>
                {
                    for (var i = start; i < stop; i++)
                    {
                        if (list[i] > threshold)
                        {
                            return list[i];
                        }
                    }

                    return null;
                };

            var connectionManager = new FakeConnectionManager();
            var dt1 = Bluepath.Threading.DistributedThread<Func<List<int>, int, int, int, int?>>.Create(
                function,
                connectionManager,
                null, 
                Threading.DistributedThread.ExecutorSelectionMode.LocalOnly);
            dt1.Start(listToProcess, 0, listToProcess.Count, 5);
            dt1.Join();

            Convert.ToInt32(dt1.Result).ShouldBe(6);
        }

        [TestMethod]
        public void ExecutesOnRemoteThread()
        {
            Func<object[], object> function = (parameters) =>
            {
                var a = (int)parameters[0];
                var b = (int)parameters[1];

                return a + b;
            };

            var remoteExecutorId = Guid.Empty;
            var serviceMock = new Mock<Bluepath.ServiceReferences.IRemoteExecutorService>(MockBehavior.Strict);
            var result  = new ServiceReferences.RemoteExecutorServiceResult()
                        {
                            ExecutorState = ServiceReferences.ExecutorState.Finished,
                            Result = 6
                        };

            serviceMock.Setup(rs => rs.Initialize(It.IsAny<byte[]>()))
                .Returns(() => 
                {
                    remoteExecutorId = Guid.NewGuid();
                    return remoteExecutorId;
                });
            serviceMock.Setup(rs => rs.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<object[]>(), It.IsAny<Bluepath.ServiceReferences.ServiceUri>()))
                .Returns(() => Task.Run(() => Services.RemoteExecutorService.GetRemoteExecutor(remoteExecutorId).Pulse(result)));
            serviceMock.Setup(rs => rs.TryJoin(It.IsAny<Guid>())).Returns(result);

            var schedulerMock = new Mock<IScheduler>(MockBehavior.Strict);
            schedulerMock.Setup(s => s.GetRemoteService()).Returns(serviceMock.Object);

            var dt1 = Bluepath.Threading.DistributedThread<Func<object[], object>>.Create(
                function,
                new ConnectionManager(remoteServices: null, listener: null),
                schedulerMock.Object,
                Threading.DistributedThread.ExecutorSelectionMode.RemoteOnly);
            dt1.Start(4, 2);
            dt1.Join();

            Convert.ToInt32(dt1.Result).ShouldBe(6);
        }

        [TestMethod]
        public void ExecutorShouldAlwaysGetDeepCopiedParameters()
        {
            Func<TestInteger, TestInteger, int> function = (__a, __b) =>
                {
                    var sum = __a.Value + __b.Value;
                    __a.Value++;
                    return sum;
                };

            var connectionManager = new FakeConnectionManager();
            var dt1 = Bluepath.Threading.DistributedThread<Func<TestInteger, TestInteger, int>>.Create(
                function,
                connectionManager,
                null,
                Threading.DistributedThread.ExecutorSelectionMode.LocalOnly);

            var a = new TestInteger() { Value = 2 };
            var b = new TestInteger() { Value = 5 };

            dt1.Start(a, b);
            dt1.Join();

            Convert.ToInt32(dt1.Result).ShouldBe(7);
            a.Value.ShouldBe(2);
            b.Value.ShouldBe(5);
        }

        [TestMethod]
        public void DistributedThreadFallbacksToLocalExecutionIfNoRemoteServiceIsAvailable()
        {
            var listToProcess = new List<int>()
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9
            };

            Func<List<int>, int, int, int, int?> function = (list, start, stop, threshold) =>
            {
                for (var i = start; i < stop; i++)
                {
                    if (list[i] > threshold)
                    {
                        return list[i];
                    }
                }

                return null;
            };

            var connectionManager = new FakeConnectionManager();
            var dt1 = Bluepath.Threading.DistributedThread<Func<List<int>, int, int, int, int?>>.Create(
                function,
                null,
                new RoundRobinLocalScheduler(new ServiceUri[0]),
                Threading.DistributedThread.ExecutorSelectionMode.RemoteOrLocal);
            dt1.Start(listToProcess, 0, listToProcess.Count, 5);
            dt1.Join();

            Convert.ToInt32(dt1.Result).ShouldBe(6);
        }

        [Serializable]
        public class TestInteger
        {
            public int Value { get; set; }
        }


        public class FakeConnectionManager : IConnectionManager
        {
            public IListener Listener { get; private set; }

            public IDictionary<ServiceUri, PerformanceStatistics> RemoteServices { get; private set; }
        }
    }
}
