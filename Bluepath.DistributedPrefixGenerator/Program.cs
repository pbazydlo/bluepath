using Bluepath.Framework;
using Bluepath.Services;
using Bluepath.Storage;
using Bluepath.Storage.Structures.Collections;
using Bluepath.Threading;
using Bluepath.Threading.Schedulers;
using Bluepath.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.DistributedPrefixGenerator
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
                            RunTest(connectionManager, scheduler, options);
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
                        var data = new List<string>(dataSize);
                        var list = new DistributedList<string>(bluepath.Storage as IExtendedStorage, key);
                        if (list.Count != dataSize)
                        {
                            list.Clear();
                        }
                        else
                        {
                            return dataSize;
                        }

                        for (int i = 0; i < dataSize; i++)
                        {
                            int nextSourceDocument = i % SourceDocuments.Documents.Count;
                            data.Add(SourceDocuments.Documents[nextSourceDocument]);
                        }

                        Console.WriteLine("Start saving data to redis");
                        list.AddRange(data);
                        return dataSize;
                    }),
                    connectionManager, scheduler, DistributedThread.ExecutorSelectionMode.LocalOnly);
            var inputDataKey = "distributedPrefixData";
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
                new Func<bool, string, int, int, IBluepathCommunicationFramework, byte[]>(
                    (returnPrefixes, inputKey, indexStart, indexEnd, bluepath)
                        =>
                    {
                        var inputList = new DistributedList<string>(bluepath.Storage as IExtendedStorage, inputKey);
                        var inputToProcess = new string[indexEnd - indexStart];
                        inputList.CopyPartTo(indexStart, inputToProcess.Length, inputToProcess);

                        List<string> results = new List<string>();
                        for (int x = indexStart; x < indexEnd; x++)
                        {
                            var documentLine = inputToProcess[x - indexStart];
                            var words = documentLine.Split(' ');
                            var partialResult = new List<string>();
                            foreach (var word in words)
                            {
                                var stringToProcess = word;
                                for (int si = 0; si < stringToProcess.Length; si++)
                                {
                                    string res = "";
                                    if (si == stringToProcess.Length - 1)
                                    {
                                        res = stringToProcess;
                                    }
                                    else
                                    {
                                        res = stringToProcess.Substring(0, si + 1);
                                    }

                                    partialResult.Add(res);
                                }
                            }
                            

                            results.AddRange(partialResult);
                            if (results.Count > 100000)
                            {
                                results.Clear();
                            }
                        }

                        if (returnPrefixes)
                        {
                            return new PrefixesResult()
                            {
                                Prefixes = results
                            }.Serialize();
                        }
                        else
                        {
                            return new PrefixesResult().Serialize();
                        }
                    }),
                connectionManager,
                scheduler
                );
                thread.Start(options.ReturnPrefixes == 1, inputDataKey, startIndex, endIndex);
                threads.Add(thread);
            }

            foreach (var thread in threads)
            {
                thread.Join();
                if (thread.State == Executor.ExecutorState.Faulted)
                {
                    Console.WriteLine("Err");
                }

                // Could process prefixes
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
            Console.WriteLine(sw.ElapsedMilliseconds);
        }
    }

    [Serializable]
    public class PrefixesResult
    {
        public List<string> Prefixes { get; set; }
    }
}
