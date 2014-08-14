using Bluepath.Framework;
using Bluepath.Services;
using Bluepath.Storage;
using Bluepath.Storage.Structures.Collections;
using Bluepath.Threading;
using Bluepath.Threading.Schedulers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.DistributedPI
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Bluepath.Log.DistributedMemoryHost = options.RedisHost;
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
            var numberOfElements = options.NoOfElements;
            var numberOfShards = options.NoOfShards;
            var threads = new List<DistributedThread>();
            Log.TraceMessage(Log.Activity.Custom, "Running test");
            var sw = new Stopwatch();
            sw.Start();
            var processFunc = new Func<int, long>(
                    (amount)
                        =>
                    {
                        var r = new Random();
                        long result = 0;
                        for (int j = 0; j < amount; j++)
                        {
                            var x = r.NextDouble();
                            var y = r.NextDouble();
                            if (x * x + y * y < 1.0)
                            {
                                result++;
                            }
                        }

                        return result;
                    });
            for (int i = 0; i < numberOfShards; i++)
            {
                var thread = DistributedThread.Create(
                    processFunc,
                    connectionManager,
                    scheduler
                    );
                thread.Start(numberOfElements);
                threads.Add(thread);
            }

            long sum = 0;
            foreach (var thread in threads)
            {
                thread.Join();
                if (thread.State == Executor.ExecutorState.Faulted)
                {
                    Console.WriteLine("Err");
                }
                else
                {
                    sum += (long)thread.Result;
                }
            }

            var pi = 4.0 * sum / (numberOfElements * numberOfShards);
            sw.Stop();
            Console.WriteLine(string.Format("PI: {0}", pi));
            Console.WriteLine(sw.ElapsedMilliseconds);
        }
    }
}
