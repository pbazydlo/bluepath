﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bluepath.Storage.Redis;
using Bluepath.Storage.Structures.Collections;
using Shouldly;
using System.Threading;
using System.Collections.Generic;

namespace Bluepath.Tests.Integration.Storage.Structures.Collections
{
    [TestClass]
    public class DistributedListTests
    {
        private static System.Diagnostics.Process redisProcess;
        private const string Host = "localhost";

        [ClassInitialize]
        public static void FixtureSetup(Microsoft.VisualStudio.TestTools.UnitTesting.TestContext tc)
        {
            redisProcess = TestHelpers.SpawnRemoteService(0, TestHelpers.ServiceType.Redis);
        }

        [TestMethod]
        public void DistributedListAllowsAddingAndRemovingItems()
        {
            var storage = new RedisStorage(Host);
            var key = Guid.NewGuid().ToString();
            var list = new DistributedList<int>(storage, key);

            for(int i=0;i<10;i++)
            {
                list.Add(i);
            }

            list.Count.ShouldBe(10);
            list.Remove(5);
            list.Count.ShouldBe(9);
            foreach (var item in list)
            {
                item.ShouldNotBe(5);
            }

        }

        [TestMethod]
        public void DistributedListAllowsAddingEnumerablesInOneStep()
        {
            var storage = new RedisStorage(Host);
            var key = Guid.NewGuid().ToString();
            var list = new DistributedList<int>(storage, key);
            var localList = new List<int>();

            for (int i = 0; i < 10; i++)
            {
                localList.Add(i);
            }

            list.AddRange(localList);
            list.Count.ShouldBe(10);
        }

        [TestMethod]
        public void DistributedListsWithTheSameKeyShareState()
        {
            var storage = new RedisStorage(Host);
            var key = Guid.NewGuid().ToString();
            var list1 = new DistributedList<int>(storage, key);

            for (int i = 0; i < 10; i++)
            {
                list1.Add(i);
            }

            list1.Count.ShouldBe(10);
            var list2 = new DistributedList<int>(storage, key);
            list2.Count.ShouldBe(10);
        }

        [TestMethod]
        public void DistributedListsPostponeRemovingItemsUntilEnumarationIsFinished()
        {
            var storage = new RedisStorage(Host);
            var key = Guid.NewGuid().ToString();
            var list1 = new DistributedList<int>(storage, key);
            var synchLock = new object();
            bool finishedRemoving = false;
            bool isEnumerating = false;
            int count = 0;

            for (int i = 0; i < 10; i++)
            {
                list1.Add(i);
            }

            var enumerationThread = new Thread(() =>
            {
                var storage1 = new RedisStorage(Host);
                var list = new DistributedList<int>(storage1, key);
                bool isFirstTurn = true;
                int localCount = 0;
                foreach (var item in list)
                {
                    localCount++;
                    isEnumerating = true;
                    if(isFirstTurn)
                    {
                        isFirstTurn = false;
                        lock(synchLock)
                        {
                            Monitor.Wait(synchLock);
                        }
                    }
                }

                count = localCount;
            });
            var removeItemThread = new Thread(() =>
            {
                var storage2 = new RedisStorage(Host);
                var list = new DistributedList<int>(storage2, key);
                list.RemoveAt(4);
                finishedRemoving = true;
            });

            enumerationThread.Start();
            TestHelpers.RepeatUntilTrue(() => isEnumerating, times: 5);
            removeItemThread.Start();
            Thread.Sleep(10);
            list1.Count.ShouldBe(10);
            finishedRemoving.ShouldBe(false);
            lock(synchLock)
            {
                Monitor.Pulse(synchLock);
            }

            enumerationThread.Join();
            removeItemThread.Join();
            finishedRemoving.ShouldBe(true);
            list1.Count.ShouldBe(9);
            count.ShouldBe(10);
        }
    }
}