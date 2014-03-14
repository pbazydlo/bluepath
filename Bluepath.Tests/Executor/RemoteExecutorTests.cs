namespace Bluepath.Tests.Executor
{
    using System;
    using System.Threading.Tasks;

    using Bluepath.Executor;
    using Bluepath.ServiceReferences;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Shouldly;

    [TestClass]
    public class RemoteExecutorTests
    {
        /// <summary>
        /// Timeout for waiting on join in milliseconds
        /// </summary>
        private const int WaitTimeout = 100;

        [TestMethod]
        public void RemoteExecutorJoinWaitsForPulse()
        {
            var remoteServiceMock = new Mock<IRemoteExecutorService>(MockBehavior.Strict);
            
            const string MethodResult = "whatever";
            var expectedResult = new RemoteExecutorServiceResult()
                {
                    ExecutorState = ServiceReferences.ExecutorState.Finished,
                    Result = MethodResult
                };

            remoteServiceMock.Setup(rs => rs.Initialize(It.IsAny<byte[]>())).Returns(() => Guid.NewGuid());
            remoteServiceMock.Setup(rs => rs.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<object[]>(), It.IsAny<ServiceUri>())).Returns(() => Task.Run(() => { }));
            remoteServiceMock.Setup(rs => rs.TryJoin(It.IsAny<Guid>())).Returns(expectedResult);
            var executor = new RemoteExecutor();
            executor.Setup(remoteServiceMock.Object, new ServiceUri());
            executor.Initialize(() => MethodResult);
            executor.ExecutorState.ShouldBe(Bluepath.Executor.ExecutorState.NotStarted);
            executor.Execute(new object[0] { });
            executor.ExecutorState.ShouldBe(Bluepath.Executor.ExecutorState.Running);
            var joinTask = Task.Run(() =>
                {
                    executor.Join();
                });

            joinTask.Wait(WaitTimeout).ShouldBe(false);
            executor.Pulse(expectedResult);
            joinTask.Wait(WaitTimeout).ShouldBe(true);

            executor.ExecutorState.ShouldBe(Bluepath.Executor.ExecutorState.Finished);
            executor.Result.ShouldBe(MethodResult);
        }
        [TestMethod]
        public void RemoteExecutorJoinsAfterPulse()
        {
            var remoteServiceMock = new Mock<IRemoteExecutorService>(MockBehavior.Strict);

            const string MethodResult = "whatever";
            var expectedResult = new RemoteExecutorServiceResult()
            {
                ExecutorState = ServiceReferences.ExecutorState.Finished,
                Result = MethodResult
            };

            remoteServiceMock.Setup(rs => rs.Initialize(It.IsAny<byte[]>())).Returns(() => Guid.NewGuid());
            remoteServiceMock.Setup(rs => rs.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<object[]>(), It.IsAny<ServiceUri>())).Returns(() => Task.Run(() => { }));
            remoteServiceMock.Setup(rs => rs.TryJoin(It.IsAny<Guid>())).Returns(expectedResult);
            var executor = new RemoteExecutor();
            executor.Setup(remoteServiceMock.Object, new ServiceUri());
            executor.Initialize(() => MethodResult);
            executor.ExecutorState.ShouldBe(Bluepath.Executor.ExecutorState.NotStarted);
            executor.Execute(new object[0] { });
            executor.ExecutorState.ShouldBe(Bluepath.Executor.ExecutorState.Running);

            executor.Pulse(expectedResult);
            executor.Join();

            executor.ExecutorState.ShouldBe(Bluepath.Executor.ExecutorState.Finished);
            executor.Result.ShouldBe(MethodResult);
        }
    }
}