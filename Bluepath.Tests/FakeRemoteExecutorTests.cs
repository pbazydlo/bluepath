namespace Bluepath.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Bluepath.Exceptions;
    using Bluepath.Executor;
    using Bluepath.ServiceReferences;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Shouldly;

    [TestClass]
    public class FakeRemoteExecutorTests
    {
        [TestMethod]
        public void FakeRemoteExecutorJoinTest()
        {
            var testMethod = new Func<int, int, int>((a, b) => { Thread.Sleep(50); return a + b; });

            var executor = new TestRemoteExecutor();
            executor.Initialize(testMethod);
            executor.Execute(new object[] { 1, 2 });

            executor.Join();
            executor.ExecutorState.ShouldBe(Executor.ExecutorState.Finished);

            var result = executor.GetResult();
            result.ShouldBe(3); // (1 + 2)
        }

        [TestMethod]
        public void FakeRemoteExecutorJoinWithExceptionTest()
        {
            var testMethod = new Func<int, int, int>((a, b) => { Thread.Sleep(50); throw new Exception("test"); });

            var executor = new TestRemoteExecutor();
            executor.Initialize(testMethod);
            executor.Execute(new object[] { 1, 2 });

            try
            {
                executor.Join();
                Assert.Fail("Exception was expected but not thrown.");
            }
            catch (Exception ex)
            {
                executor.ExecutorState.ShouldBe(Executor.ExecutorState.Faulted);

                if (ex is RemoteException)
                {
                    ex.InnerException.InnerException.Message.ShouldBe("test");
                }
                else
                {
                    Assert.Fail(string.Format("RemoteException was expected but another ('{0}') was thrown.", ex.GetType()));
                }
            }
        }

        internal class TestRemoteExecutor : RemoteExecutor
        {
            protected override void Initialize()
            {
                this.Client = new FakeRemoteExecutorService();
            }
        }

        protected class FakeRemoteExecutorService : Bluepath.Services.RemoteExecutorService, Bluepath.ServiceReferences.IRemoteExecutorService
        {
            public async Task<Guid> InitializeAsync(byte[] methodHandle)
            {
                return this.Initialize(methodHandle);
            }

            public async Task ExecuteAsync(Guid eId, object[] parameters)
            {
                this.Execute(eId, parameters);
            }

            public RemoteExecutorServiceResult TryJoin(Guid eId)
            {
                var baseResult = base.TryJoin(eId);
                var result = new RemoteExecutorServiceResult();

                result.ElapsedTime = baseResult.ElapsedTime;
                result.Error = baseResult.Error;
                result.ExecutorState = (Bluepath.ServiceReferences.ExecutorState)baseResult.ExecutorState;
                result.Result = baseResult.Result;

                return result;
            }

            public async Task<RemoteExecutorServiceResult> TryJoinAsync(Guid eId)
            {
                return this.TryJoin(eId);
            }
        }
    }
}
