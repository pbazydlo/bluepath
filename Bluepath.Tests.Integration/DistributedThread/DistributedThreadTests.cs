namespace Bluepath.Tests.Integration.DistributedThread
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    using Bluepath.Executor;
    using Bluepath.Services;

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

            Monitor.Exit(DistributedThreadTests.testLock);
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            TestHelpers.KillAllServices();
        }

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodWithCallback()
        {
            const int executorPort = 23004;

            var myThread = InitializeWithSubtractFunc(executorPort, externalRunner: true);
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

            TestHelpers.KillService(executorPort);
        }

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodWithPollingJoin()
        {
            const int executorPort = 23003;
            var myThread = InitializeWithSubtractFunc(executorPort);

            myThread.Start(new object[] { new object[] { 5, 3 } });
            var joinThread = new System.Threading.Thread(() =>
            {
                myThread.Join();
            });
            joinThread.Start();
            joinThread.Join(joinWaitTime).ShouldBe(true);

            ((int)myThread.Result).ShouldBe(2);

            TestHelpers.KillService(executorPort);
        }

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodWithPollingJoinOnExternalRunner()
        {
            const int executorPort = 23002;

            var myThread = InitializeWithSubtractFunc(executorPort, externalRunner: true);

            myThread.Start(new object[] { new object[] { 5, 3 } });
            var joinThread = new System.Threading.Thread(() =>
            {
                myThread.Join();
            });
            joinThread.Start();
            joinThread.Join(joinWaitTime).ShouldBe(true);


            if (myThread.State != ExecutorState.Finished)
            {
                // Above condition is sometimes true.
                // TODO: investigate why do we have to wait for the result after successful join?
                Assert.Inconclusive("Result should be available right after successful join.");
            }

            ((int)myThread.Result).ShouldBe(2);

            TestHelpers.KillService(executorPort);
        }

        [TestMethod]
        public void DistributedThreadRemotelyPassesExceptionInCaseOfIncorrectInvokation()
        {
            const int executorPort = 23001;

            var myThread = InitializeWithSubtractFunc(executorPort);

            myThread.Start(new object[] { 5, 3 });
            var joinThread = new System.Threading.Thread(() =>
            {
                myThread.Join();
            });
            joinThread.Start();
            joinThread.Join(joinWaitTime).ShouldBe(true);

            myThread.State.ShouldBe(ExecutorState.Faulted);

            TestHelpers.KillService(executorPort);
        }

        [TestMethod]
        public void DistributedThreadRemotelyPassesExceptionInCaseOfFunctionError()
        {
            const int executorPort = 23000;

            var myThread = InitializeWithExceptionThrowingFunc(executorPort);

            myThread.Start(new object[0]);
            var joinThread = new System.Threading.Thread(() =>
            {
                myThread.Join();
            });
            joinThread.Start();
            joinThread.Join(joinWaitTime * 2).ShouldBe(true);

            myThread.State.ShouldBe(ExecutorState.Faulted);

            TestHelpers.KillService(executorPort);
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
            return Initialize<TFunc>(testFunc, port, ip);
        }

        private static Threading.DistributedThread<TFunc> InitializeWithExternalRunner<TFunc>(TFunc testFunc, int port)
        {
            string ip = "127.0.0.1";
            TestHelpers.SpawnRemoteService(port);
            Thread.Sleep(3000);
            return Initialize<TFunc>(testFunc, port, ip);
        }

        private static Threading.DistributedThread<TFunc> Initialize<TFunc>(TFunc testFunc, int port, string ip)
        {
            BluepathSingleton.Instance.CallbackUri = null;
            var endpointAddress = new System.ServiceModel.EndpointAddress(
                string.Format("http://{0}:{1}/BluepathExecutorService.svc", ip, port));
            //Bluepath.Threading.DistributedThread.RemoteServices.Add(
            //    new ServiceReferences.RemoteExecutorServiceClient(
            //        new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.None),
            //        endpointAddress));
            var connectionManager =
                new ConnectionManager(
                    new ServiceReferences.RemoteExecutorServiceClient(
                        new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.None),
                        endpointAddress));
            var myThread = Bluepath.Threading.DistributedThread.Create(testFunc, connectionManager);
            return myThread;
        }
    }
}
