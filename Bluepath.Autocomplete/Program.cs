using Bluepath.Framework;
using Bluepath.Services;
using Bluepath.Storage;
using Bluepath.Storage.Redis;
using Bluepath.Storage.Structures;
using Bluepath.Storage.Structures.Collections;
using Bluepath.Threading;
using Bluepath.Threading.Schedulers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Autocomplete
{
    class Program
    {
        private static object prefixesDictionaryLock = new object();
        public static Dictionary<string, List<string>> Prefixes { get; set; }

        static void Main(string[] args)
        {
            Program.Prefixes = new Dictionary<string, List<string>>();
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Bluepath.Log.RedisHost = options.RedisHost;
                Bluepath.Log.WriteInfoToConsole = false;
                Bluepath.Log.WriteToRedis = false;

                var bluepathListener = Bluepath.Services.BluepathListener.InitializeDefaultListener(options.Ip, options.Port);
                using (var serviceDiscoveryClient = new CentralizedDiscovery.Client.CentralizedDiscovery(
                    new ServiceUri(options.CentralizedDiscoveryURI, BindingType.BasicHttpBinding),
                    bluepathListener
                    ))
                {
                    using (var connectionManager = new ConnectionManager(
                        remoteService: null,
                        listener: bluepathListener,
                        serviceDiscovery: serviceDiscoveryClient,
                        serviceDiscoveryPeriod: TimeSpan.FromSeconds(1)))
                    {
                        System.Threading.Thread.Sleep(1500);

                        string command = string.Empty;
                        string sharedStorageKey = "loadlist";
                        var sharedCounterKey = "counter";

                        Console.WriteLine("Initializing Redis");
                        var storage = new RedisStorage(options.RedisHost);
                        var list = new DistributedList<string>(storage, sharedStorageKey);
                        Console.WriteLine("List count: {0}", list.Count);

                        var commands = new Dictionary<string, Action>()
                        {
                            {"LOAD", ()=>
                            {
                                var sw = new Stopwatch();
                                sw.Start();
                                var scheduler = new RoundRobinLocalScheduler(connectionManager.RemoteServices.Select(s=>s.Key).ToArray());
                                list.Clear();
                                var counter = new DistributedCounter(storage, sharedCounterKey);
                                counter.SetValue(0);
                                var localList = new List<string>();
                                foreach(var file in Directory.EnumerateFiles(options.InputFolder))
                                {
                                    var fileContent = File.ReadAllText(file);
                                    localList.Add(fileContent);
                                }

                                list.AddRange(localList);
                                localList.Clear();
                                var servicesCount = connectionManager.RemoteServices.Count;
                                if(servicesCount==0)
                                {
                                    servicesCount = 1;
                                }

                                var calculatedChunkSize = (int)Math.Floor(Convert.ToDouble(list.Count) / servicesCount);
                                if(calculatedChunkSize==0)
                                {
                                    calculatedChunkSize = 1;
                                }

                                var threads = new List<DistributedThread>();
                                for(int i=0;i<servicesCount;i++)
                                {
                                    var loadThread = DistributedThread.Create(
                                    new Func<string, string, int, IBluepathCommunicationFramework, int>(
                                        (inputKey, counterKey, chunkSize, bluepath) =>
                                        {
                                            return LoadDocuments(inputKey, counterKey, chunkSize, bluepath);
                                        }), connectionManager, scheduler);
                                    loadThread.Start(sharedStorageKey, sharedCounterKey, calculatedChunkSize);
                                    threads.Add(loadThread);
                                }

                                foreach (var thread in threads)
                                {
                                    thread.Join();
                                }

                                list.Clear();
                                sw.Stop();
                                Console.WriteLine("Loaded in {0}ms", sw.ElapsedMilliseconds);
                            }},
                            {"SEARCH", ()=>
                            {
                                var services = connectionManager.RemoteServices.Select(s => s.Key).ToArray();
                                var scheduler = new RoundRobinLocalScheduler(services);
                                var threads = new List<DistributedThread>();
                                Console.Write("Word part: ");
                                var query = Console.ReadLine();
                                var sw = new Stopwatch();
                                sw.Start();
                                for(int i = 0;(i<services.Length) || i < 1;i++)
                                {
                                    var searchThread = DistributedThread.Create(
                                        new Func<string,string[]>((searchPhraze) =>
                                            {
                                                List<string> result = new List<string>();
                                                lock(Program.prefixesDictionaryLock)
                                                {
                                                    if(Program.Prefixes.ContainsKey(searchPhraze))
                                                    {
                                                        result = Program.Prefixes[searchPhraze];
                                                    }
                                                }

                                                return result.ToArray();
                                            }), null, scheduler);
                                    searchThread.Start(query);
                                    threads.Add(searchThread);
                                }

                                var endResult = new List<string>();
                                foreach (var thread in threads)
                                {
                                    thread.Join();
                                    endResult.AddRange(thread.Result as string[]);
                                }

                                var distinctResult = endResult.Distinct().ToArray();
                                sw.Stop();

                                Console.WriteLine();
                                Console.WriteLine(string.Join("||", distinctResult));
                                Console.WriteLine("Found in {0}ms", sw.ElapsedMilliseconds);
                                
                            }},
                            {"CLEAN", ()=>
                            {
                                var services = connectionManager.RemoteServices.Select(s => s.Key).ToArray();
                                var scheduler = new RoundRobinLocalScheduler(services);
                                var threads = new List<DistributedThread>();
                                for(int i = 0;(i<services.Length) || i < 1;i++)
                                {
                                    var searchThread = DistributedThread.Create(
                                        new Func<int>(() =>
                                            {
                                                List<string> result = null;
                                                lock(Program.prefixesDictionaryLock)
                                                {
                                                    Program.Prefixes.Clear();
                                                }

                                                return 0;
                                            }), null, scheduler);
                                    searchThread.Start();
                                    threads.Add(searchThread);
                                }

                                foreach (var thread in threads)
                                {
                                    thread.Join();
                                }
                            }}
                        };
                        do
                        {
                            Console.WriteLine("Available commands:");
                            foreach (var key in commands.Keys)
                            {
                                Console.WriteLine(key);
                            }

                            Console.Write("Command['q' to exit]: ");
                            command = Console.ReadLine().ToUpper();
                            if (commands.ContainsKey(command))
                            {
                                commands[command]();
                            }

                        } while (command != "Q");
                    }
                }
            }
        }

        private static int LoadDocuments(string inputKey, string counterKey, int chunkSize, IBluepathCommunicationFramework bluepath)
        {
            var inputList = new DistributedList<string>(bluepath.Storage as IExtendedStorage, inputKey);
            var inputCount = inputList.Count;
            var counter = new DistributedCounter(bluepath.Storage as IExtendedStorage, counterKey);
            int indexEnd = 0;
            do
            {
                int noOfElements = chunkSize;
                int indexStart = counter.GetAndIncrease(chunkSize);
                if (indexStart >= inputCount)
                {
                    break;
                }

                indexEnd = indexStart + noOfElements;
                if (indexEnd > inputCount)
                {
                    indexEnd = inputCount;
                    noOfElements = indexEnd - indexStart;
                }

                var inputDocuments = new string[noOfElements];
                inputList.CopyPartTo(indexStart, noOfElements, inputDocuments);
                foreach (var document in inputDocuments)
                {
                    var words = document.Split(' ');
                    foreach (var word in words)
                    {
                        if (word.Length == 0)
                        {
                            continue;
                        }

                        var stringToProcess = word;
                        var partialResult = new List<string>(stringToProcess.Length);
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

                        lock (Program.prefixesDictionaryLock)
                        {
                            foreach (var prefix in partialResult)
                            {
                                if (Program.Prefixes.ContainsKey(prefix))
                                {
                                    if (!Program.Prefixes[prefix].Contains(word))
                                    {
                                        Program.Prefixes[prefix].Add(word);
                                    }
                                }
                                else
                                {
                                    Program.Prefixes.Add(prefix, new List<string>() { word });
                                }
                            }
                        }
                    }
                }

            } while (indexEnd <= inputCount);

            return 0;
        }
    }
}
