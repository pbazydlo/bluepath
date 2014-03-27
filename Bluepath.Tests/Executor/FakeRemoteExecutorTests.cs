namespace Bluepath.Tests.Executor
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Bluepath.Exceptions;
    using Bluepath.Executor;
    using Bluepath.Extensions;
    using Bluepath.Framework;
    using Bluepath.ServiceReferences;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Shouldly;

    using ExecutorState = Bluepath.Executor.ExecutorState;

    [TestClass]
    public class FakeRemoteExecutorTests
    {
        [TestMethod]
        public void FakeRemoteExecutorJoinTest()
        {
            const int DelayMilliseconds = 50;
            var testMethod = new Func<int, int, int, int>((a, b, delay) => { Thread.Sleep(delay); return a + b; });

            testMethod.Method.IsStatic.ShouldBe(true);

            var executor = new RemoteExecutor();
            executor.Setup(new FakeRemoteExecutorService(), null);
            executor.Initialize(testMethod);
            executor.ExecutorState.ShouldBe(ExecutorState.NotStarted);

            executor.Execute(new object[] { 1, 2, DelayMilliseconds });
            executor.ExecutorState.ShouldBe(ExecutorState.Running);

            executor.Join();
            executor.ExecutorState.ShouldBe(ExecutorState.Finished);

            var result = executor.GetResult();
            result.ShouldBe(3); // (1 + 2)
            executor.ElapsedTime.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(DelayMilliseconds));
        }

        [TestMethod]
        public void FakeRemoteExecutorCanUseCommunicationFrameworkWithOneMissingParameterTest()
        {
            var testMethod = new Func<IBluepathCommunicationFramework, int, Guid>((bluepath, a) => bluepath.ProcessEid);

            testMethod.Method.IsStatic.ShouldBe(true);

            var executor = new RemoteExecutor();
            executor.Setup(new FakeRemoteExecutorService(), null);
            executor.Initialize(testMethod);
            var eid = executor.Eid;
            executor.Execute(new object[] { 1 });
            executor.Join();

            var result = executor.GetResult();
            result.ShouldBe(eid);
            Assert.IsTrue((Guid)result != Guid.Empty);
        }

        [TestMethod]
        public void FakeRemoteExecutorCanUseCommunicationFrameworkSubsitutingParameterTest()
        {
            var testMethod = new Func<IBluepathCommunicationFramework, int, Guid>((bluepath, a) => bluepath.ProcessEid);

            testMethod.Method.IsStatic.ShouldBe(true);

            var executor = new RemoteExecutor();
            executor.Setup(new FakeRemoteExecutorService(), null);
            executor.Initialize(testMethod);
            var eid = executor.Eid;
            executor.Execute(new object[] { null, 1 });
            executor.Join();

            var result = executor.GetResult();
            result.ShouldBe(eid);
            Assert.IsTrue((Guid)result != Guid.Empty);
        }

        [TestMethod]
        public async Task FakeRemoteExecutorAsyncJoinTest()
        {
            const int DelayMilliseconds = 50;
            var testMethod = new Func<int, int, int, int>((a, b, delay) => { Thread.Sleep(delay); return a + b; });

            testMethod.Method.IsStatic.ShouldBe(true);

            var executor = new RemoteExecutor();
            executor.Setup(new FakeRemoteExecutorService(), null);
            executor.Initialize(testMethod);
            executor.ExecutorState.ShouldBe(ExecutorState.NotStarted);

            executor.Execute(new object[] { 1, 2, DelayMilliseconds });
            executor.ExecutorState.ShouldBe(ExecutorState.Running);

            await executor.JoinAsync();
            executor.ExecutorState.ShouldBe(ExecutorState.Finished);

            var result = executor.GetResult();
            result.ShouldBe(3); // (1 + 2)
            executor.ElapsedTime.ShouldBeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(DelayMilliseconds));
        }

        [TestMethod]
        public void FakeRemoteExecutorJoinWithExceptionTest()
        {
            var testMethod = new Func<int, int, int>((a, b) => { throw new Exception("test"); });

            var executor = new RemoteExecutor();
            executor.Setup(new FakeRemoteExecutorService(), null);
            executor.Initialize(testMethod);
            executor.Execute(new object[] { 1, 2 });

            try
            {
                executor.Join();
                Assert.Fail("Exception was expected but not thrown.");
            }
            catch (Exception ex)
            {
                executor.ExecutorState.ShouldBe(ExecutorState.Faulted);

                if (ex is RemoteException)
                {
                    ex.InnerException.Message.Contains("System.Reflection.TargetInvocationException").ShouldBe(true);
                    ex.InnerException.Message.Contains("System.Exception: test").ShouldBe(true);
                }
                else
                {
                    Assert.Fail(string.Format("RemoteException was expected but another ('{0}') was thrown on local site.", ex.GetType()));
                }
            }
        }

        protected class FakeRemoteExecutorService : Bluepath.Services.RemoteExecutorService, Bluepath.ServiceReferences.IRemoteExecutorService
        {
            // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
            // ReSharper disable once CSharpWarnings::CS1998
            public async Task<Guid> InitializeAsync(byte[] methodHandle)
            {
                return this.Initialize(methodHandle);
            }

            public void Execute(Guid eId, object[] parameters, ServiceUri callbackUri)
            {
                this.Execute(eId, parameters, callbackUri != null ? new Services.ServiceUri() { Address = callbackUri.Address } : null);
            }

            // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
            // ReSharper disable once CSharpWarnings::CS1998
            public async Task ExecuteAsync(Guid eId, object[] parameters, ServiceUri callbackUri)
            {
                this.Execute(eId, parameters, callbackUri);
            }

            public new RemoteExecutorServiceResult TryJoin(Guid eId)
            {
                var baseResult = base.TryJoin(eId);
                var result = new RemoteExecutorServiceResult();

                result.ElapsedTime = baseResult.ElapsedTime;
                result.Error = baseResult.Error;
                result.ExecutorState = (Bluepath.ServiceReferences.ExecutorState)baseResult.ExecutorState;
                result.Result = baseResult.Result;

                return result;
            }

            // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
            // ReSharper disable once CSharpWarnings::CS1998
            public async Task<RemoteExecutorServiceResult> TryJoinAsync(Guid eId)
            {
                return this.TryJoin(eId);
            }

            public PerformanceStatistics GetPerformanceStatistics()
            {
                throw new NotImplementedException();
            }

            public Task<PerformanceStatistics> GetPerformanceStatisticsAsync()
            {
                throw new NotImplementedException();
            }

            public void ExecuteCallback(Guid eId, RemoteExecutorServiceResult executeResult)
            {
                base.ExecuteCallback(eId, executeResult.Convert());
            }

            // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
            // ReSharper disable once CSharpWarnings::CS1998
            public async Task ExecuteCallbackAsync(Guid eId, RemoteExecutorServiceResult executeResult)
            {
                this.ExecuteCallback(eId, executeResult);
            }
        }
    }
}
