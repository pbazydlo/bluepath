namespace Bluepath.Tests.Integration.DistributedThread
{
    using System;
    using System.Diagnostics;
    using System.Runtime.Remoting.Messaging;
    using System.Threading;

    using Bluepath.Exceptions;
    using Bluepath.Executor;
    using Bluepath.Services;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Shouldly;

    using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

    [TestClass]
    public class DistributedThreadTests
    {
        private static int LastUsedPortNumber = 24500;

        private static object LastUsedPortNumberLock = new object();

        private const int JoinWaitTime = 2000;

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
        }

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodWithCallback()
        {
            var executorPort = DistributedThreadTests.GetNextPortNumber();

            const string Ip = "127.0.0.1";
            var port = DistributedThreadTests.GetNextPortNumber();

            if (this.listener != null)
            {
                throw new Exception("Test can have only one listener.");
            }

            this.listener = new BluepathListener(Ip, port);

            var myThread = this.InitializeWithSubtractFunc(executorPort, externalRunner: true, callbackListener: this.listener);

            myThread.Start(5, 3);
            var joinThread = new System.Threading.Thread(myThread.Join);
            joinThread.Start();
            joinThread.Join();

            // Result should be 5 - 3 = 2
            ((int)myThread.Result).ShouldBe(2);

            this.listener.Stop();
        }

        [TestMethod]
        public void DistributedThreadRemotelyExecutesFSharpMethodWithCallback()
        {
            var executorPort = DistributedThreadTests.GetNextPortNumber();

            const string Ip = "127.0.0.1";
            var port = DistributedThreadTests.GetNextPortNumber();

            if (this.listener != null)
            {
                throw new Exception("Test can have only one listener.");
            }

            this.listener = new BluepathListener(Ip, port);
            // Computes the sum of the squares of the numbers divisible by 3
            var testFunc = new Func<int, int>(Bluepath.Tests.Methods.DefaultModule.sumOfSquaresDivisibleBy3UpTo);

            var myThread = this.InitializeWithExternalRunner(testFunc, executorPort, this.listener);

            // n = 7 => generates [ 1 .. 7 ]
            myThread.Start(7);
            var joinThread = new System.Threading.Thread(myThread.Join);
            joinThread.Start();
            joinThread.Join();

            // Result should be 3*3 + 6*6 = 45
            ((int)myThread.Result).ShouldBe(45);

            this.listener.Stop();
        }

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodWithPollingJoin()
        {
            var executorPort = DistributedThreadTests.GetNextPortNumber();
            var myThread = this.InitializeWithSubtractFunc(executorPort, externalRunner: true);

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
            var executorPort = DistributedThreadTests.GetNextPortNumber();

            var myThread = this.InitializeWithSubtractFunc(executorPort, externalRunner: true);

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
            var executorPort = DistributedThreadTests.GetNextPortNumber();

            var myThread = this.InitializeWithSubtractFunc(executorPort, externalRunner: true);

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
            var executorPort = DistributedThreadTests.GetNextPortNumber();

            var myThread = this.InitializeWithExceptionThrowingFunc(executorPort, externalRunner: true);
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

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodTakingClassAsItsParameter()
        {
            var executorPort = DistributedThreadTests.GetNextPortNumber();

            Func<ComplexParameter, string> testFunc = (complexParameter) =>
            {
                return string.Format("{0}_{1}", complexParameter.SomeProperty, complexParameter.AnotherProperty);
            };

            var parameter = new ComplexParameter()
            {
                SomeProperty = "jack",
                AnotherProperty = 56
            };

            if (this.listener != null)
            {
                throw new Exception("Test can have only one listener.");
            }

            var listenerAndThreadTuple = Initialize(testFunc, executorPort);

            this.listener = listenerAndThreadTuple.Listener;
            var myThread = listenerAndThreadTuple.Thread;

            myThread.Start(parameter);
            var joinThread = new System.Threading.Thread(myThread.Join);
            joinThread.Start();
            joinThread.Join();

            myThread.Result.ToString().ShouldBe(string.Format("{0}_{1}", parameter.SomeProperty, parameter.AnotherProperty));
            this.listener.Stop();
        }

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodReturningClass()
        {
            var executorPort = DistributedThreadTests.GetNextPortNumber();

            Func<string, ComplexParameter> testFunc = (parameter) =>
            {
                return new ComplexParameter()
                {
                    SomeProperty = parameter,
                    AnotherProperty = 44
                };
            };

            var testValue = "jack";

            if (this.listener != null)
            {
                throw new Exception("Test can have only one listener.");
            }

            var listenerAndThreadTuple = Initialize(testFunc, executorPort);

            this.listener = listenerAndThreadTuple.Listener;
            var myThread = listenerAndThreadTuple.Thread;

            myThread.Start(testValue);
            var joinThread = new System.Threading.Thread(myThread.Join);
            joinThread.Start();
            joinThread.Join();

            var result = (ComplexParameter)myThread.Result;
            result.SomeProperty.ShouldBe(testValue);
            result.AnotherProperty.ShouldBe(44);

            this.listener.Stop();
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

        private static ListenerAndThreadTuple<TFunc> Initialize<TFunc>(TFunc testFunc, int port)
        {
            const string Ip = "127.0.0.1";
            var listener = new BluepathListener(Ip, port);

            return new ListenerAndThreadTuple<TFunc>()
                       {
                           Listener = listener,
                           Thread = Initialize(testFunc, Ip, port)
                       };
        }

        private Threading.DistributedThread<TFunc> InitializeWithExternalRunner<TFunc>(TFunc testFunc, int port, IListener callbackListener = null)
        {
            if (this.executorHostProcess != null)
            {
                throw new Exception("Test can have only one executor host process.");
            }

            const string Ip = "127.0.0.1";
            this.executorHostProcess = TestHelpers.SpawnRemoteService(port);
            return Initialize(testFunc, Ip, port, callbackListener);
        }

        private static Threading.DistributedThread<TFunc> Initialize<TFunc>(TFunc testFunc, string ip, int port, IListener callbackListener = null)
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

        private static int GetNextPortNumber()
        {
            lock (LastUsedPortNumberLock)
            {
                return ++LastUsedPortNumber;
            }
        }
    }

    internal class ListenerAndThreadTuple<TFunc>
    {
        public BluepathListener Listener { get; set; }

        public Threading.DistributedThread<TFunc> Thread { get; set; }
    }
}
