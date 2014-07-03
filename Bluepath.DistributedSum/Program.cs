using Bluepath.Services;
using Bluepath.Threading.Schedulers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluepath.DLINQ;
using Bluepath.Framework;
using Bluepath.Storage.Structures.Collections;
using Bluepath.Storage;
using Bluepath.Threading;
using System.Diagnostics;

namespace Bluepath.DistributedSum
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Bluepath.Log.RedisHost = options.RedisHost;
                Bluepath.Log.WriteInfoToConsole = false;
                var bluepathListener = new BluepathListener(options.Ip, options.Port);
                using (var serviceDiscoveryClient
                    = new CentralizedDiscovery.Client.CentralizedDiscovery(
                        new ServiceUri(options.CentralizedDiscoveryURI, BindingType.BasicHttpBinding),
                        bluepathListener
                        )
                      )
                {
                    using (var connectionManager = new ConnectionManager(
                            remoteService: null,
                            listener: bluepathListener,
                            serviceDiscovery: serviceDiscoveryClient,
                            serviceDiscoveryPeriod: TimeSpan.FromSeconds(30)))
                    {
                        System.Threading.Thread.Sleep(1500);
                        var scheduler = new ThreadNumberScheduler(connectionManager);

                        if (options.IsSlave == 0)
                        {
                            Log.TraceMessage(Log.Activity.Custom, "Running master");
                            var storage = new Bluepath.Storage.Redis.RedisStorage(options.RedisHost);
                            var list = new DistributedList<int>(storage, Guid.NewGuid().ToString());
                            var localList = new List<int>();
                            var rSw = new Stopwatch();
                            int amount = 1000;
                            rSw.Start();
                            for (int i = 0; i < amount; i++)
                            {
                                localList.Add(1);
                            }

                            list.AddRange(localList);
                            rSw.Stop();
                            var wSw = new Stopwatch();
                            int aa = 0;
                            wSw.Start();
                            for (int i = 0; i < amount; i++)
                            {
                                aa = list[i];
                            }

                            wSw.Stop();
                            Log.TraceMessage(Log.Activity.Custom, string.Format("Write: {0}; Read: {1}; Value {2}; Amount: {3}", rSw.ElapsedMilliseconds, wSw.ElapsedMilliseconds, aa, amount));
                            //RunTest(connectionManager, scheduler, options);
                        }
                        else
                        {
                            Log.TraceMessage(Log.Activity.Custom, "Running slave");
                        }

                        Console.WriteLine("Press <Enter> to stop the service.");
                        Console.ReadLine();
                    }
                }
            }
        }

        private static void RunTest(ConnectionManager connectionManager, ThreadNumberScheduler scheduler, Options options)
        {
            Log.TraceMessage(Log.Activity.Custom, "Creating data");
            var initializeDataThread = DistributedThread.Create(
                new Func<int, string, IBluepathCommunicationFramework, int>(
                    (dataSize, key, bluepath) =>
                    {
                        //var data = new List<int>(dataSize);
                        //for (int i = 0; i < dataSize; i++)
                        //{
                        //    data.Add(1);
                        //}

                        //var list = new DistributedList<int>(bluepath.Storage as IExtendedStorage, key);
                        //if (list.Count != dataSize)
                        //{
                        //    list.Clear();
                        //    list.AddRange(data);
                        //}

                        return dataSize;
                    }),
                    connectionManager, scheduler, DistributedThread.ExecutorSelectionMode.LocalOnly);
            var inputDataKey = "inputData";
            var numberOfElements = options.NoOfElements;
            initializeDataThread.Start(numberOfElements, inputDataKey);
            initializeDataThread.Join();
            var expectedSum = (int)initializeDataThread.Result;
            var numberOfShards = options.NoOfShards;
            var elementsPerShard = numberOfElements / numberOfShards;
            var threads = new List<DistributedThread>();
            Log.TraceMessage(Log.Activity.Custom, "Running test");
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < numberOfShards; i++)
            {
                int startIndex = i * elementsPerShard;
                int endIndex;
                if (i == numberOfShards - 1)
                {
                    // last element
                    endIndex = numberOfElements;
                }
                else
                {
                    endIndex = startIndex + elementsPerShard;
                }

                var thread = DistributedThread.Create(
                new Func<string, int, int, IBluepathCommunicationFramework, int>(
                    (inputKey, indexStart, indexEnd, bluepath)
                        =>
                    {
                        //var inputList = new DistributedList<int>(bluepath.Storage as IExtendedStorage, inputKey);
                        int partialSum = 0;
                        for (int x = indexStart; x < indexEnd; x++)
                        {
                            //partialSum += inputList[x];
                            System.Threading.Thread.Sleep(1000);
                        }

                        return partialSum;
                    }),
                connectionManager,
                scheduler
                );
                thread.Start(inputDataKey, startIndex, endIndex);
                threads.Add(thread);
            }

            int overallSum = 0;
            foreach (var thread in threads)
            {
                thread.Join();
                if (thread.State == Executor.ExecutorState.Faulted)
                {
                    Console.WriteLine("Err");
                }

                overallSum += (int)thread.Result;
            }

            sw.Stop();
            //var clearDataThread = DistributedThread.Create(
            //    new Func<string, IBluepathCommunicationFramework, int>(
            //        (key, bluepath) =>
            //        {
            //            var list = new DistributedList<int>(bluepath.Storage as IExtendedStorage, key);
            //            list.Clear();
            //            return 0;
            //        }),
            //        connectionManager, scheduler, DistributedThread.ExecutorSelectionMode.LocalOnly);
            //clearDataThread.Start(inputDataKey);
            //clearDataThread.Join();
            Console.WriteLine(overallSum);
            Console.WriteLine(sw.ElapsedMilliseconds);
        }
    }
}
