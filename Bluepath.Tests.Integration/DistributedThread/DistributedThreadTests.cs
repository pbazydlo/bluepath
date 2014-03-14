namespace Bluepath.Tests.Integration.DistributedThread
{
    using System;
    using System.Threading;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Shouldly;

    [TestClass]
    public class DistributedThreadTests
    {
        private static readonly int joinWaitTime = 2000;
        private static object testLock = new object();
        private static Thread serviceThread = null;

        [TestInitialize]
        public void TestSetup()
        {
            Monitor.Enter(DistributedThreadTests.testLock);
        }

        [TestCleanup]
        public void CleanUp()
        {
            if (DistributedThreadTests.serviceThread != null)
            {
                DistributedThreadTests.serviceThread.Abort();
                DistributedThreadTests.serviceThread = null;
            }

            TestHelpers.KillAllServices();
            Monitor.Exit(DistributedThreadTests.testLock);
        }

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodWithCallback()
        {
            var myThread = InitializeWithSubtractFunc(23004, externalRunner: true);
            string ip = "127.0.0.1";
            int port = 24000;
            serviceThread = new System.Threading.Thread(() =>
            {
                BluepathSingleton.Instance.Initialize(ip, port);
            });
            serviceThread.Start();
            Thread.Sleep(1000);

            myThread.Start(new object[] { new object[] { 5, 3 } });
            var joinThread = new System.Threading.Thread(myThread.Join);
            joinThread.Start();
            joinThread.Join(); // .ShouldBe(true);

            ((int)myThread.Result).ShouldBe(2);
        }

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodWithPollingJoin()
        {
            var myThread = InitializeWithSubtractFunc(23003);

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
        public void DistributedThreadRemotelyExecutesStaticMethodWithPollingJoinOnExternalRunner()
        {
            var myThread = InitializeWithSubtractFunc(23002, externalRunner: true);

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
            var myThread = InitializeWithSubtractFunc(23001);

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
            var myThread = InitializeWithExceptionThrowingFunc(23000);

            myThread.Start(new object[0]);
            var joinThread = new System.Threading.Thread(() =>
            {
                myThread.Join();
            });
            joinThread.Start();
            joinThread.Join(joinWaitTime).ShouldBe(true);

            myThread.State.ShouldBe(Executor.ExecutorState.Faulted);
        }

        private static Threading.DistributedThread<Func<object[], object>> InitializeWithSubtractFunc(int port, bool externalRunner = false)
        {
            Func<object[], object> testFunc = (parameters) =>
            {
                return ((int)parameters[0]) - ((int)parameters[1]);
            };

            if (!externalRunner)
            {
                return Initialize(testFunc, port);
            }
            else
            {
                return InitializeWithExternalRunner(testFunc, port);
            }
        }

        private static Threading.DistributedThread<Func<object[], object>> InitializeWithExceptionThrowingFunc(int port, bool externalRunner = false)
        {
            Func<object[], object> testFunc = (parameters) =>
            {
                throw new Exception("test");
            };

            if (!externalRunner)
            {
                return Initialize(testFunc, port);
            }
            else
            {
                return InitializeWithExternalRunner(testFunc, port);
            }
        }

        private static Threading.DistributedThread<TFunc> Initialize<TFunc>(TFunc testFunc, int port)
        {
            string ip = "127.0.0.1";
            var serviceThread = new System.Threading.Thread(() =>
            {
                BluepathSingleton.Instance.Initialize(ip, port);
            });
            serviceThread.Start();
            Thread.Sleep(1000);
            BluepathSingleton.Instance.CallbackUri = null;
            var endpointAddress = new System.ServiceModel.EndpointAddress(
                string.Format("http://{0}:{1}/BluepathExecutorService.svc", ip, port));
            Bluepath.Threading.DistributedThread<TFunc>.RemoteServices.Add(
                new ServiceReferences.RemoteExecutorServiceClient(
                    new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.None),
                    endpointAddress));

            var myThread = Bluepath.Threading.DistributedThread<TFunc>.Create(
                testFunc);
            return myThread;
        }

        private static Threading.DistributedThread<TFunc> InitializeWithExternalRunner<TFunc>(TFunc testFunc, int port)
        {
            string ip = "127.0.0.1";
            TestHelpers.SpawnRemoteService(port);
            Thread.Sleep(3000);
            BluepathSingleton.Instance.CallbackUri = null;
            var endpointAddress = new System.ServiceModel.EndpointAddress(
                string.Format("http://{0}:{1}/BluepathExecutorService.svc", ip, port));
            Bluepath.Threading.DistributedThread<TFunc>.RemoteServices.Add(
                new ServiceReferences.RemoteExecutorServiceClient(
                    new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.None),
                    endpointAddress));

            var myThread = Bluepath.Threading.DistributedThread<TFunc>.Create(
                testFunc);
            return myThread;
        }
    }
}
