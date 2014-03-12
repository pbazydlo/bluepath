using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using Bluepath.Executor;
using Moq;
using Bluepath.ServiceReferences;
using ExecutorState = Bluepath.Executor.ExecutorState;
using System.Threading.Tasks;

namespace Bluepath.Tests.Executor
{
    [TestClass]
    public class RemoteExecutorTests
    {
        /// <summary>
        /// Timeout for waiting on join in ms
        /// </summary>
        private int waitTimeout = 100;

        [TestMethod]
        public void RemoteExecutorJoinWaitsForPulse()
        {
            
            var remoteServiceMock = new Mock<IRemoteExecutorService>(MockBehavior.Strict);
            
            var methodResult = "whatever";
            var expectedResult = new RemoteExecutorServiceResult()
                {
                    ExecutorState = ServiceReferences.ExecutorState.Finished,
                    Result = methodResult
                };

            remoteServiceMock.Setup(rs => rs.InitializeAsync(It.IsAny<byte[]>())).Returns(() => Task.Run(() => Guid.NewGuid()));
            remoteServiceMock.Setup(rs => rs.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<object[]>())).Returns(() => Task.Run(() => { }));
            remoteServiceMock.Setup(rs => rs.TryJoin(It.IsAny<Guid>())).Returns(expectedResult);
            var executor = new RemoteExecutor();
            executor.Initialize(remoteServiceMock.Object, () => "whatever");
            executor.ExecutorState.ShouldBe(ExecutorState.NotStarted);
            executor.Execute(new object[0] { });
            executor.ExecutorState.ShouldBe(ExecutorState.Running);
            var joinTask = Task.Run(() =>
                {
                    executor.Join();
                });

            joinTask.Wait(waitTimeout).ShouldBe(false);
            executor.Pulse(expectedResult);
            joinTask.Wait(waitTimeout).ShouldBe(true);

            executor.ExecutorState.ShouldBe(ExecutorState.Finished);
            executor.Result.ShouldBe(methodResult);
        }
        [TestMethod]
        public void RemoteExecutorJoinsAfterPulse()
        {

            var remoteServiceMock = new Mock<IRemoteExecutorService>(MockBehavior.Strict);

            var methodResult = "whatever";
            var expectedResult = new RemoteExecutorServiceResult()
            {
                ExecutorState = ServiceReferences.ExecutorState.Finished,
                Result = methodResult
            };

            remoteServiceMock.Setup(rs => rs.InitializeAsync(It.IsAny<byte[]>())).Returns(() => Task.Run(() => Guid.NewGuid()));
            remoteServiceMock.Setup(rs => rs.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<object[]>())).Returns(() => Task.Run(() => { }));
            remoteServiceMock.Setup(rs => rs.TryJoin(It.IsAny<Guid>())).Returns(expectedResult);
            var executor = new RemoteExecutor();
            executor.Initialize(remoteServiceMock.Object, () => "whatever");
            executor.ExecutorState.ShouldBe(ExecutorState.NotStarted);
            executor.Execute(new object[0] { });
            executor.ExecutorState.ShouldBe(ExecutorState.Running);

            executor.Pulse(expectedResult);
            executor.Join();

            executor.ExecutorState.ShouldBe(ExecutorState.Finished);
            executor.Result.ShouldBe(methodResult);
        }

    }
}
