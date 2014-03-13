using System;
using Shouldly;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Bluepath.Tests.Acceptance.DistributedThread
{
    [TestClass]
    public class DistributedThreadTests
    {
        private static readonly int joinWaitTime = 2000;

        [TestCleanup]
        public void CleanUp()
        {
            TestHelpers.KillAllServices();
        }

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodWithPollingJoin()
        {
            var myThread = InitializeWithSubstructFunc();

            myThread.Start(new object[] { new object[] { 5, 3 } });
            var joinThread = new System.Threading.Thread(() =>
            {
                myThread.Join();
            });
            joinThread.Start();
            joinThread.Join(joinWaitTime).ShouldBe(true);

            ((int)myThread.Result).ShouldBe(2);
        }

        [TestMethod]
        public void DistributedThreadRemotelyPassesExceptionInCaseOfIncorrectInvokation()
        {
            var myThread = InitializeWithSubstructFunc();

            myThread.Start(new object[] { 5, 3 });
            var joinThread = new System.Threading.Thread(() =>
            {
                myThread.Join();
            });
            joinThread.Start();
            joinThread.Join(joinWaitTime).ShouldBe(true);

            myThread.State.ShouldBe(Executor.ExecutorState.Faulted);
        }

        [TestMethod]
        public void DistributedThreadRemotelyPassesExceptionInCaseOfFunctionError()
        {
            var myThread = InitializeWithExceptionThrowingFunc();

            myThread.Start(new object[0]);
            var joinThread = new System.Threading.Thread(() =>
            {
                myThread.Join();
            });
            joinThread.Start();
            joinThread.Join(joinWaitTime).ShouldBe(true);

            myThread.State.ShouldBe(Executor.ExecutorState.Faulted);
        }

        private static Threading.DistributedThread InitializeWithSubstructFunc()
        {
            Func<object[], object> testFunc = (parameters) =>
            {
                return ((int)parameters[0]) - ((int)parameters[1]);
            };

            return Initialize(testFunc);
        }

        private static Threading.DistributedThread InitializeWithExceptionThrowingFunc()
        {
            Func<object[], object> testFunc = (parameters) =>
            {
                throw new Exception("test");
            };

            return Initialize(testFunc);
        }

        private static Threading.DistributedThread Initialize(Func<object[], object> testFunc)
        {
            string ip = "127.0.0.1";
            int port = 23654;
            var serviceThread = new System.Threading.Thread(() =>
            {
                BluepathSingleton.Instance.Initialize(ip, port);
            });
            serviceThread.Start();
            Thread.Sleep(1000);
            BluepathSingleton.Instance.CallbackUri = null;
            var endpointAddress = new System.ServiceModel.EndpointAddress(
                string.Format("http://{0}:{1}/BluepathExecutorService.svc", ip, port)
                );
            Bluepath.Threading.DistributedThread.RemoteServices.Add(
                new ServiceReferences.RemoteExecutorServiceClient(
                    new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.None),
                    endpointAddress));

            var myThread = Bluepath.Threading.DistributedThread.Create(
                testFunc);
            return myThread;
        }
    }
}
