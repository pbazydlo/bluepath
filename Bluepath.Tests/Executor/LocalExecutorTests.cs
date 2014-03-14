namespace Bluepath.Tests.Executor
{
    using System;
    using System.Threading;

    using Bluepath.Executor;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Shouldly;

    [TestClass]
    public class LocalExecutorTests
    {
        [TestMethod]
        public void LocalExecutorTimedOutJoinTest()
        {
            var testMethod = new Func<object[], object>(
                (parameters) =>
                    {
                        while (true)
                        {
                            Thread.Sleep(10);
                        }
                    });

            var executor = new LocalExecutor();
            executor.InitializeNonGeneric(testMethod);
            executor.Execute(null);
            executor.Join(timeout: new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 30));

            executor.ElapsedTime.ShouldBeLessThan(new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 3, milliseconds: 0));
            executor.Exception.GetType().ShouldBe(typeof(ThreadAbortException));
        }
    }
}
