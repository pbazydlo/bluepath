namespace Bluepath.Tests
{
    using System;

    using global::Bluepath.Executor;
    using global::Bluepath.Services;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Shouldly;

    [TestClass]
    public class RemoteExecutorServiceTests
    {
        [TestMethod]
        public void RemoteExecutorServiceExecuteTest()
        {
            // we have generic method handler serializer, so we can use strongly typed functions like this:
            var testMethod = new Func<int, int, int>((a, b) => a + b);

            var service = new RemoteExecutorService();
            var serializedMethodHandle = global::Bluepath.Executor.MethodHandleSerializer.GetSerializedMethodHandle(testMethod);
            var eid = service.Initialize(serializedMethodHandle);

            // the following method was private, but it should be public to allow this kind of test
            var executor = RemoteExecutorService.GetExecutor(eid);

            service.Execute(eid, new object[] { 1, 2 });

            // wait for the worker thread to complete
            executor.Join();

            // use method exposed by the service to get computation state
            var result = service.TryJoin(eid);

            result.ExecutorState.ShouldBe(RemoteExecutorServiceResult.State.Finished);
            result.Result.ShouldBe(3);
            result.Error.ShouldBe(null);
            result.ElapsedTime.HasValue.ShouldBe(true);
        }

        [TestMethod]
        public void RemoteExecutorServiceExceptionInUserCodeShouldGetCaughtTest()
        {
            var testMethod = new Func<object>(() => { throw new Exception("test"); });

            var service = new RemoteExecutorService();
            var serializedMethodHandle = MethodHandleSerializer.GetSerializedMethodHandle(testMethod);
            var eid = service.Initialize(serializedMethodHandle);
            var executor = RemoteExecutorService.GetExecutor(eid);

            service.Execute(eid, null);

            // wait for the worker thread to complete
            executor.Join();

            // use method exposed by the service to get computation state
            var result = service.TryJoin(eid);

            result.ExecutorState.ShouldBe(RemoteExecutorServiceResult.State.Faulted);
            result.Error.InnerException.Message.ShouldBe("test");
            result.ElapsedTime.HasValue.ShouldBe(true);
        }
    }
}
