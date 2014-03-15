namespace Bluepath.Tests.Integration.DistributedThread
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    using Bluepath.Exceptions;
    using Bluepath.Executor;
    using Bluepath.Services;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Shouldly;

    [TestClass]
    public class DistributedThreadTests
    {
        private static readonly int joinWaitTime = 2000;
        private static Thread serviceThread = null;

        private Process executorHostProcess;

        [TestInitialize]
        public void TestSetup()
        {
        }

        [TestCleanup]
        public void CleanUp()
        {
            try
            {
                if (this.executorHostProcess != null)
                {
                    this.executorHostProcess.Kill();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Couldn't kill process ({0}).", ex.Message);
            }

            if (DistributedThreadTests.serviceThread != null)
            {
                DistributedThreadTests.serviceThread.Abort();
                DistributedThreadTests.serviceThread = null;
            }
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            // TestHelpers.KillAllServices();
        }

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodWithCallback()
        {
            const int executorPort = 23004;

            var myThread = this.InitializeWithSubtractFunc(executorPort, externalRunner: true);
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
            const int executorPort = 23003;
            var myThread = this.InitializeWithSubtractFunc(executorPort, externalRunner: true);

            myThread.Start(new object[] { new object[] { 5, 3 } });
            var joinThread = new System.Threading.Thread(() =>
            {
                myThread.Join();
            });
            joinThread.Start();
            joinThread.Join(joinWaitTime).ShouldBe(true);

            if (myThread.State == ExecutorState.Running)
            {
                Assert.Inconclusive("Result should be available right after successful join.");
            }

            ((int)myThread.Result).ShouldBe(2);
        }

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodWithPollingJoinOnExternalRunner()
        {
            const int executorPort = 23002;

            var myThread = this.InitializeWithSubtractFunc(executorPort, externalRunner: true);

            myThread.Start(new object[] { new object[] { 5, 3 } });
            var joinThread = new System.Threading.Thread(() =>
            {
                myThread.Join();
            });
            joinThread.Start();
            joinThread.Join(joinWaitTime).ShouldBe(true);


            if (myThread.State != ExecutorState.Finished)
            {
                Assert.Inconclusive("Result should be available right after successful join.");
            }

            ((int)myThread.Result).ShouldBe(2);
        }

        [TestMethod]
        public void DistributedThreadRemotelyPassesExceptionInCaseOfIncorrectInvokation()
        {
            const int executorPort = 23001;

            var myThread = this.InitializeWithSubtractFunc(executorPort, externalRunner: true);

            myThread.Start(new object[] { 5, 3 });
            var exception = default(Exception);
            var joinThread = new System.Threading.Thread(() =>
            {
                try
                {
                    myThread.Join();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });
            joinThread.Start();
            joinThread.Join(joinWaitTime).ShouldBe(true);

            if (myThread.State == ExecutorState.Running)
            {
                Assert.Inconclusive("Exception should be available right after successful join.");
            }

            Assert.IsNotNull(exception);
            exception.GetType().ShouldBe(typeof(RemoteException));
            myThread.State.ShouldBe(ExecutorState.Faulted);
        }

        [TestMethod]
        public void DistributedThreadRemotelyPassesExceptionInCaseOfFunctionError()
        {
            const int executorPort = 23000;

            var myThread = this.InitializeWithExceptionThrowingFunc(executorPort, externalRunner: true);
            var exception = default(Exception);
            myThread.Start(new object[0]);
            var joinThread = new System.Threading.Thread(() =>
            {
                try
                {
                    myThread.Join();
                }
                catch(Exception ex)
                {
                    exception = ex;
                }
            });
            joinThread.Start();
            joinThread.Join(joinWaitTime * 2).ShouldBe(true);

            if (myThread.State == ExecutorState.Running)
            {
                Assert.Inconclusive("Exception should be available right after successful join.");
            }

            Assert.IsNotNull(exception);
            exception.GetType().ShouldBe(typeof(RemoteException));
            myThread.State.ShouldBe(ExecutorState.Faulted);
        }

        private Threading.DistributedThread<Func<object[], object>> InitializeWithSubtractFunc(int port, bool externalRunner = false)
        {
            Func<object[], object> testFunc = (parameters) =>
            {
                return ((int)parameters[0]) - ((int)parameters[1]);
            };

            if (!externalRunner)
            {
                throw new NotSupportedException("Test must be performed using external runner.");
            }
            else
            {
                return this.InitializeWithExternalRunner(testFunc, port);
            }
        }

        private Threading.DistributedThread<Func<object[], object>> InitializeWithExceptionThrowingFunc(int port, bool externalRunner = false)
        {
            Func<object[], object> testFunc = (parameters) =>
            {
                throw new Exception("test");
            };

            if (!externalRunner)
            {
                throw new NotSupportedException("Test must be performed using external runner.");
            }
            else
            {
                return this.InitializeWithExternalRunner(testFunc, port);
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

        private Threading.DistributedThread<TFunc> InitializeWithExternalRunner<TFunc>(TFunc testFunc, int port)
        {
            if (this.executorHostProcess != null)
            {
                throw new Exception("Test can have only one executor host process.");
            }

            string ip = "127.0.0.1";
            this.executorHostProcess = TestHelpers.SpawnRemoteService(port);
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
