﻿namespace Bluepath.Tests.DistributedThread
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Moq;

    using Shouldly;

    [TestClass]
    public class DistributedThreadTests
    {
        [TestMethod]
        public void ExecutesSingleThread()
        {
            var listToProcess = new List<int>()
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9
            };

            Func<List<int>, int, int, int, int?> t1Action = (list, start, stop, threshold) =>
                {
                    for (int i = start; i < stop; i++)
                    {
                        if (list[i] > threshold)
                        {
                            return list[i];
                        }
                    }

                    return null;
                };

            var dt1 = Bluepath.Threading.DistributedThread.Create(
                (parameters) => t1Action(parameters[0] as List<int>, (int)parameters[1], (int)parameters[2], (int)parameters[3]),
                Threading.DistributedThread.ExecutorSelectionMode.LocalOnly
                );
            dt1.Start(new object[] { listToProcess, 0, listToProcess.Count, 5 });
            dt1.Join();

            Convert.ToInt32(dt1.Result).ShouldBe(6);
        }

        [TestMethod]
        public void ExecutesOnRemoteThread()
        {
            Func<object[], object> t1Action = (parameters) =>
            {
                int a = (int)parameters[0];
                int b = (int)parameters[1];

                return a + b;
            };

            Guid remoteExecutorId = Guid.Empty;
            var serviceMock = new Mock<Bluepath.ServiceReferences.IRemoteExecutorService>(MockBehavior.Strict);
            serviceMock.Setup(rs => rs.Initialize(It.IsAny<byte[]>()))
                .Returns(() => 
                {
                    remoteExecutorId = Guid.NewGuid();
                    return remoteExecutorId;
                });
            serviceMock.Setup(rs => rs.ExecuteAsync(It.IsAny<Guid>(), It.IsAny<object[]>(), It.IsAny<Bluepath.ServiceReferences.ServiceUri>()))
                .Returns(() => Task.Run(() => 
                {
                    Services.RemoteExecutorService.GetRemoteExecutor(remoteExecutorId).Pulse(new ServiceReferences.RemoteExecutorServiceResult()
                        {
                            ExecutorState = ServiceReferences.ExecutorState.Finished,
                            Result = 6
                        });
                }));
            Bluepath.Threading.DistributedThread.RemoteServices.Add(serviceMock.Object);
            BluepathSingleton.Instance.CallbackUri = new Services.ServiceUri();
            var dt1 = Bluepath.Threading.DistributedThread.Create(
                t1Action,
                Threading.DistributedThread.ExecutorSelectionMode.RemoteOnly
                );
            dt1.Start(new object[] { 4, 2 });
            dt1.Join();

            Convert.ToInt32(dt1.Result).ShouldBe(6);
        }
    }
}
