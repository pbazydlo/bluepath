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
                var bluepathListener = new BluepathListener(options.Ip, options.Port);
                using (var serviceDiscoveryClient
                    = new CentralizedDiscovery.Client.CentralizedDiscovery(
                        new ServiceUri(options.CentralizedDiscoveryURI, BindingType.BasicHttpBinding),
                        bluepathListener
                        )
                      )
                {
                    var connectionManager = new ConnectionManager(
                            remoteService: null,
                            listener: bluepathListener,
                            serviceDiscovery: serviceDiscoveryClient);
                    var scheduler = new ThreadNumberScheduler(connectionManager);
                    var thread = DistributedThread.Create(
                        new Func<string,string, int, int, IBluepathCommunicationFramework, int>(
                            (inputKey, resultKey, indexStart, indexEnd, bluepath)
                                => 
                                {
                                    var inputList = new DistributedList<int>(bluepath.Storage as IExtendedStorage, inputKey);
                                    int partialSum = 0;
                                    for (int i = indexStart; i < indexEnd; i++)
                                    {
                                        partialSum += inputList[i];
                                    }

                                    return partialSum;
                                }),
                        connectionManager,
                        scheduler
                        );

                    var initializeDataThread = DistributedThread.Create(
                        new Func<int, string, IBluepathCommunicationFramework, int>(
                            (dataSize, key, bluepath)=>
                            {
                                var data = new List<int>(dataSize);
                                for(int i=0;i<dataSize;i++)
                                {
                                    data.Add(1);
                                }

                                var list = new DistributedList<int>(bluepath.Storage as IExtendedStorage, key);
                                list.AddRange(data);
                                return dataSize;
                            }),
                            connectionManager, scheduler, DistributedThread.ExecutorSelectionMode.LocalOnly);
                    var inputDataKey = "inputData";
                    initializeDataThread.Start(100000, inputDataKey);
                    initializeDataThread.Join();
                    var expectedSum = (int)initializeDataThread.Result;
                        

                }
            }
        }
    }
}
