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
        private const int JoinWaitTime = 2000;

        private static Thread serviceThread = null;

        private Process executorHostProcess;

        private BluepathListener listener;

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

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodWithCallback()
        {
            const int ExecutorPort = 23004;

            const string Ip = "127.0.0.1";
            const int Port = 24000;

            if (this.listener != null)
            {
                throw new Exception("Test can have only one listener.");
            }

            this.listener = new BluepathListener(Ip, Port);
            
            var myThread = this.InitializeWithSubtractFunc(ExecutorPort, externalRunner: true, callbackListener: this.listener);
            
            Thread.Sleep(1000);

            myThread.Start(5, 3);
            var joinThread = new System.Threading.Thread(myThread.Join);
            joinThread.Start();
            joinThread.Join();

            // Result should be 5 - 3 = 2
            ((int)myThread.Result).ShouldBe(2);

            this.listener.Stop();
        }

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodWithPollingJoin()
        {
            const int ExecutorPort = 23003;
            var myThread = this.InitializeWithSubtractFunc(ExecutorPort, externalRunner: true);

            myThread.Start(5, 3);
            var joinThread = new System.Threading.Thread(myThread.Join);
            joinThread.Start();
            var joinCompletedBeforeTimeout = joinThread.Join(JoinWaitTime);

            if (!joinCompletedBeforeTimeout)
            {
                Assert.Inconclusive("Join takes longer than {0} ms. Test aborted.", JoinWaitTime);
            }

            if (myThread.State == ExecutorState.Running)
            {
                Assert.Inconclusive("Result should be available right after successful join.");
            }

            // Result should be 5 - 3 = 2
            ((int)myThread.Result).ShouldBe(2);
        }

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodWithPollingJoinOnExternalRunner()
        {
            const int ExecutorPort = 23002;

            var myThread = this.InitializeWithSubtractFunc(ExecutorPort, externalRunner: true);

            myThread.Start(5, 3);
            var joinThread = new System.Threading.Thread(myThread.Join);
            joinThread.Start();
            var joinCompletedBeforeTimeout = joinThread.Join(JoinWaitTime);

            if (!joinCompletedBeforeTimeout)
            {
                Assert.Inconclusive("Join takes longer than {0} ms. Test aborted.", JoinWaitTime);
            }

            if (myThread.State != ExecutorState.Finished)
            {
                Assert.Inconclusive("Result should be available right after successful join.");
            }

            // Result should be 5 - 3 = 2
            ((int)myThread.Result).ShouldBe(2);
        }

        [TestMethod]
        public void DistributedThreadRemotelyPassesExceptionInCaseOfIncorrectInvokation()
        {
            const int ExecutorPort = 23001;

            var myThread = this.InitializeWithSubtractFunc(ExecutorPort, externalRunner: true);

            // Function expects two parameters, but we are passing only one to test exception handling
            myThread.Start(5); 
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
            var joinCompletedBeforeTimeout = joinThread.Join(JoinWaitTime);

            if (!joinCompletedBeforeTimeout)
            {
                Assert.Inconclusive("Join takes longer than {0} ms. Test aborted.", JoinWaitTime);
            }

            if (myThread.State == ExecutorState.Running)
            {
                Assert.Inconclusive("Exception should be available right after successful join.");
            }

            Assert.IsNotNull(exception);
            exception.GetType().ShouldBe(typeof(RemoteException));
            myThread.State.ShouldBe(ExecutorState.Faulted);
            Assert.IsTrue(exception.InnerException.Message.Contains("System.Reflection.TargetParameterCountException"), "TargetParameterCountException was expected but another '{0}' was thrown", exception.InnerException.Message);
        }

        [TestMethod]
        public void DistributedThreadRemotelyPassesExceptionInCaseOfFunctionError()
        {
            const int ExecutorPort = 23000;

            var myThread = this.InitializeWithExceptionThrowingFunc(ExecutorPort, externalRunner: true);
            var exception = default(Exception);
            myThread.Start();
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
            var joinCompletedBeforeTimeout = joinThread.Join(JoinWaitTime * 2);
            if (!joinCompletedBeforeTimeout)
            {
                Assert.Inconclusive("Join takes longer than {0} ms. Test aborted.", JoinWaitTime * 2);
            }

            if (myThread.State == ExecutorState.Running)
            {
                Assert.Inconclusive("Exception should be available right after successful join.");
            }

            Assert.IsNotNull(exception);
            exception.GetType().ShouldBe(typeof(RemoteException));
            myThread.State.ShouldBe(ExecutorState.Faulted);
        }

        private Threading.DistributedThread<Func<int, int, int>> InitializeWithSubtractFunc(int port, bool externalRunner = false, IListener callbackListener = null)
        {
            Func<int, int, int> testFunc = (a, b) =>
            {
                Thread.Sleep(50);
                return a - b;
            };

            if (!externalRunner)
            {
                throw new NotSupportedException("Test must be performed using external runner.");
            }
            else
            {
                return this.InitializeWithExternalRunner(testFunc, port, callbackListener);
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

        private Threading.DistributedThread<TFunc> Initialize<TFunc>(TFunc testFunc, int port)
        {
            const string Ip = "127.0.0.1";
            if (this.listener != null)
            {
                throw new Exception("Test can have only one listener.");
            }

            serviceThread = new System.Threading.Thread(() =>
            {
                this.listener = new BluepathListener(Ip, port);
            });
            serviceThread.Start();

            return this.Initialize<TFunc>(testFunc, Ip, port);
        }

        private Threading.DistributedThread<TFunc> InitializeWithExternalRunner<TFunc>(TFunc testFunc, int port, IListener callbackListener = null)
        {
            if (this.executorHostProcess != null)
            {
                throw new Exception("Test can have only one executor host process.");
            }

            const string Ip = "127.0.0.1";
            this.executorHostProcess = TestHelpers.SpawnRemoteService(port);
            return this.Initialize<TFunc>(testFunc, Ip, port, callbackListener);
        }

        private Threading.DistributedThread<TFunc> Initialize<TFunc>(TFunc testFunc, string ip, int port, IListener callbackListener = null)
        {
            var endpointAddress = new System.ServiceModel.EndpointAddress(
                string.Format("http://{0}:{1}/BluepathExecutorService.svc", ip, port));

            var connectionManager = new ConnectionManager(
                    new ServiceReferences.RemoteExecutorServiceClient(
                        new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.None),
                        endpointAddress),
                    listener: callbackListener);
            var myThread = Bluepath.Threading.DistributedThread.Create(testFunc, connectionManager);
            return myThread;
        }
    }
}
