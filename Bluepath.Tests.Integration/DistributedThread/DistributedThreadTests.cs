using System;
using Shouldly;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;

namespace Bluepath.Tests.Acceptance.DistributedThread
{
    [TestClass]
    public class DistributedThreadTests
    {
        [TestCleanup]
        public void CleanUp()
        {
            TestHelpers.KillAllServices();
        }

        [TestMethod]
        public void DistributedThreadRemotelyExecutesStaticMethodWithPollingJoin()
        {
            string ip = "127.0.0.1";
            int port = 23654;

            // var isServiceSpawned = TestHelpers.SpawnRemoteService(port);
            // isServiceSpawned.ShouldBe(true);
            var serviceThread = new System.Threading.Thread(() =>
            {
                BluepathSingleton.Instance.Initialize(ip, port);
            });
            serviceThread.Start();
            Thread.Sleep(2000);
            BluepathSingleton.Instance.CallbackUri = null;
            Func<object[], object> testFunc = (parameters) =>
                {
                    return ((int)parameters[0]) - ((int)parameters[1]);
                };
            var endpointAddress = new System.ServiceModel.EndpointAddress(
                string.Format("http://{0}:{1}/BluepathExecutorService.svc", ip, port)
                );
            Bluepath.Threading.DistributedThread.RemoteServices.Add(
                new ServiceReferences.RemoteExecutorServiceClient(
                    new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.None),
                    endpointAddress));

            var myThread = Bluepath.Threading.DistributedThread.Create(
                testFunc);

            //myThread.Start(new object[] { new object[] { 5, 3 } });
            myThread.Start(new object[] { 5, 3 });
            var joinThread = new System.Threading.Thread(() =>
            {
                myThread.Join();
            });
            joinThread.Start();
            joinThread.Join();//.ShouldBe(true);

            ((int)myThread.Result).ShouldBe(2);
        }
    }
}
