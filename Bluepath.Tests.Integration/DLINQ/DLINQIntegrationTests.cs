﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bluepath.DLINQ;
using Bluepath.Services;
using Bluepath.Storage.Redis;
using Shouldly;

namespace Bluepath.Tests.Integration.DLINQ
{
    [TestClass]
    public class DLINQIntegrationTests
    {
        private static int StartPort = 25500;

        private static System.Diagnostics.Process redisProcess;

        private const string Host = "localhost";

        private static void PrepareDLINQEnviroment(out BluepathListener listener1, out BluepathListener listener2, out ConnectionManager connectionManager)
        {
            listener1 = new BluepathListener("127.0.0.1", StartPort);
            listener2 = new BluepathListener("127.0.0.1", StartPort + 1);
            var availableServices = new Dictionary<ServiceUri, PerformanceStatistics>()
                {
                    {listener1.CallbackUri, new PerformanceStatistics()
                    {
                        NumberOfTasks = new Dictionary<Bluepath.Executor.ExecutorState, int>()
                        {
                            {Bluepath.Executor.ExecutorState.Faulted, 0},
                            {Bluepath.Executor.ExecutorState.Finished, 0},
                            {Bluepath.Executor.ExecutorState.NotStarted, 0},
                            {Bluepath.Executor.ExecutorState.Running, 0},
                        }
                    }},
                    {listener2.CallbackUri, new PerformanceStatistics()
                    {
                        NumberOfTasks = new Dictionary<Bluepath.Executor.ExecutorState, int>()
                        {
                            {Bluepath.Executor.ExecutorState.Faulted, 0},
                            {Bluepath.Executor.ExecutorState.Finished, 0},
                            {Bluepath.Executor.ExecutorState.NotStarted, 0},
                            {Bluepath.Executor.ExecutorState.Running, 0},
                        }
                    }},
                };
            connectionManager = new ConnectionManager(availableServices, listener1);
        }

        [ClassInitialize]
        public static void FixtureSetup(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext tc)
        {
            redisProcess = TestHelpers.SpawnRemoteService(0, TestHelpers.ServiceType.Redis);

        }

        [TestMethod]
        public void DLINQPerformsDistributedSelect()
        {
            BluepathListener listener1;
            BluepathListener listener2;
            ConnectionManager connectionManager;
            PrepareDLINQEnviroment(out listener1, out listener2, out connectionManager);

            try
            {
                var inputCollection = new List<int>(100);
                for (int i = 0; i < 100; i++)
                {
                    inputCollection.Add(i);
                }

                var expectedSum = (from val in inputCollection
                                   select val * 2).Sum();

                var storage = new RedisStorage(Host);

                var processedCollection = inputCollection.AsDistributed(storage, connectionManager)
                    .Select(val => val * 2);
                int sum = 0;
                foreach (var val in processedCollection)
                {
                    sum += val;
                }

                sum.ShouldBe(expectedSum);
            }
            finally
            {
                listener1.Stop();
                listener2.Stop();
            }
        }

        [TestMethod]
        public void DLINQPerformsDistributedWhere()
        {
            BluepathListener listener1;
            BluepathListener listener2;
            ConnectionManager connectionManager;
            PrepareDLINQEnviroment(out listener1, out listener2, out connectionManager);

            try
            {
                var inputCollection = new List<int>(100);
                for (int i = 0; i < 100; i++)
                {
                    inputCollection.Add(i);
                }

                var expectedCount = (from val in inputCollection
                                     where val % 2 == 0
                                     select val).Count();

                var storage = new RedisStorage(Host);

                var processedCollection = inputCollection.AsDistributed(storage, connectionManager)
                    .Where(val => val % 2 == 0);
                int count = 0;
                foreach (var val in processedCollection)
                {
                    count++;
                }

                count.ShouldBe(expectedCount);
            }
            finally
            {
                listener1.Stop();
                listener2.Stop();
            }
        }

        [TestMethod]
        public void DLINQPerformsDistributedSelectMany()
        {
            BluepathListener listener1;
            BluepathListener listener2;
            ConnectionManager connectionManager;
            PrepareDLINQEnviroment(out listener1, out listener2, out connectionManager);

            try
            {
                var inputCollection = new List<string>()
                    {
                        "jack",
                        "checked",
                        "chicken",
                        "in",
                        "the",
                        "kitchen"
                    };

                var expectedResult = inputCollection.SelectMany(word => word.ToCharArray()).ToList();

                var storage = new RedisStorage(Host);

                var processedCollection = inputCollection.AsDistributed(storage, connectionManager)
                    .SelectMany(word => word.ToCharArray()).ToList();

                for (int i = 0; i < processedCollection.Count; i++)
                {
                    processedCollection[i].ShouldBe(expectedResult[i]);
                }
            }
            finally
            {
                listener1.Stop();
                listener2.Stop();
            }
        }

        [TestMethod]
        public void DLINQPerformsDistributedSelectManyWithIndex()
        {
            BluepathListener listener1;
            BluepathListener listener2;
            ConnectionManager connectionManager;
            PrepareDLINQEnviroment(out listener1, out listener2, out connectionManager);

            try
            {
                var inputCollection = new List<string>()
                    {
                        "jack",
                        "checked",
                        "chicken",
                        "in",
                        "the",
                        "kitchen"
                    };

                var expectedResult = inputCollection.SelectMany((word, i) => (string.Format("{0}{1}", i, word)).ToCharArray()).ToList();

                var storage = new RedisStorage(Host);

                var processedCollection = inputCollection.AsDistributed(storage, connectionManager)
                    .SelectMany((word, i) => (string.Format("{0}{1}", i, word)).ToCharArray()).ToList();

                for (int i = 0; i < processedCollection.Count; i++)
                {
                    processedCollection[i].ShouldBe(expectedResult[i]);
                }
            }
            finally
            {
                listener1.Stop();
                listener2.Stop();
            }
        }

        [TestMethod]
        public void DLINQPerformsDistributedSelectManyWithResultSelector()
        {
            BluepathListener listener1;
            BluepathListener listener2;
            ConnectionManager connectionManager;
            PrepareDLINQEnviroment(out listener1, out listener2, out connectionManager);

            try
            {
                var inputCollection = new List<string>()
                    {
                        "jack",
                        "checked",
                        "chicken",
                        "in",
                        "the",
                        "kitchen"
                    };

                var expectedResult = inputCollection.SelectMany(word => word.ToCharArray(), (word, character) => string.Format("{0} - {1}", word, character)).ToList();

                var storage = new RedisStorage(Host);

                var processedCollection = inputCollection.AsDistributed(storage, connectionManager)
                    .SelectMany(word => word.ToCharArray(), (word, character) => string.Format("{0} - {1}", word, character)).ToList();

                for (int i = 0; i < processedCollection.Count; i++)
                {
                    processedCollection[i].ShouldBe(expectedResult[i]);
                }
            }
            finally
            {
                listener1.Stop();
                listener2.Stop();
            }
        }
    }
}