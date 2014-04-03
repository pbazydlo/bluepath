namespace Bluepath.MapReduce.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Bluepath.Framework;
    using Bluepath.Services;
    using Bluepath.Storage;
    using Bluepath.Threading;
    using Bluepath.Threading.Schedulers;

    using NetReduce;
    using NetReduce.Core;

    public class Coordinator
    {
        private readonly IConnectionManager connectionManager;
        private readonly IScheduler scheduler;
        private readonly DistributedThread.ExecutorSelectionMode executorSelectionMode;
        private List<DistributedThread> mapWorkers;
        private List<DistributedThread> reduceWorkers;
        private List<string> keys;

        public Coordinator()
        {
        }

        public Coordinator(IConnectionManager connectionManager, IScheduler scheduler, DistributedThread.ExecutorSelectionMode executorSelectionMode)
        {
            this.connectionManager = connectionManager;
            this.scheduler = scheduler;
            this.executorSelectionMode = executorSelectionMode;
        }

        public void Start(int maxMapperNo, int maxReducerNo, FileUri mapFuncFileName, FileUri reduceFuncFileName, IEnumerable<FileUri> filesToProcess)
        {
            maxMapperNo = filesToProcess.Count();
            this.mapWorkers = this.InitMapThreads(1, maxMapperNo);
            this.keys = this.PerformMap(mapFuncFileName, filesToProcess);
            this.keys = this.keys.Distinct().OrderBy(k => k).ToList();
            this.reduceWorkers = this.InitReduceThreads(maxMapperNo + 1, maxReducerNo);
            var reducersAssignment = this.TransferIntermediateFiles(this.keys);
            this.PerformReduce(reduceFuncFileName, reducersAssignment);
        }

        private List<DistributedThread> InitMapThreads(int startWorkerNo, int workersCount)
        {
            var mapFunc = new Func<string, string, IBluepathCommunicationFramework, IEnumerable<string>>((filePath, mapFileName, bluepath) =>
                {
                    if (!(bluepath.Storage is IExtendedStorage))
                    {
                        throw new Exception("Mapper requires IExtendedStorage features.");
                    }

                    var storage = new BluepathStorage(bluepath.Storage as IExtendedStorage);

                    var mapProvider = Loader.Load<IMapProvider>(mapFileName, storage);
                    var mapper = new Mapper(filePath.ToString(), mapProvider.Map, storage);
                    var mapResult = mapper.PerformMap();

                    var keys = mapResult.Select(r => r.Key).Distinct().OrderBy(k => k);
                    foreach (var res in mapResult)
                    {
                        Log.TraceMessage(string.Format("[Map] Storing key '{0}', value '{1}'", res.Key, res.Value));
                        bluepath.Storage.StoreOrUpdate(res.Key, res.Value);
                    }

                    return keys;
                });

            return this.InitThread(startWorkerNo, workersCount, mapFunc);
        }

        private List<DistributedThread> InitReduceThreads(int startWorkerNo, int workersCount)
        {
            var reduceFunc = new Func<string, string, IBluepathCommunicationFramework, IEnumerable<string>>((filePath, reduceFuncName, bluepath) =>
            {
                if (!(bluepath.Storage is IExtendedStorage))
                {
                    throw new Exception("Reducer requires IExtendedStorage features.");
                }

                var storage = new BluepathStorage(bluepath.Storage as IExtendedStorage);

                var mapProvider = Loader.Load<IReduceProvider>(reduceFuncName, storage);
                var mapper = new Reducer(filePath.ToString(), mapProvider.Reduce, storage);
                var reduceResult = mapper.PerformReduce();

                Log.TraceMessage(string.Format("[Reduce] Storing key '{0}', value '{1}'", reduceResult.Key, reduceResult.Value));
                bluepath.Storage.StoreOrUpdate(reduceResult.Key, reduceResult.Value);

                return new List<string>() { reduceResult.Key };
            });

            return this.InitThread(startWorkerNo, workersCount, reduceFunc);
        }

        private List<DistributedThread> InitThread(int startWorkerNo, int workersCount, Func<string, string, IBluepathCommunicationFramework, IEnumerable<string>> func)
        {
            var workers = new List<DistributedThread>();
            for (int i = 0; i < workersCount; i++)
            {
                var worker = default(DistributedThread);
                if (this.connectionManager == null || this.scheduler == null)
                {
                    worker = DistributedThread.Create(func);
                }
                else
                {
                    worker = DistributedThread.Create(func, this.connectionManager, this.scheduler, this.executorSelectionMode);
                }

                // worker.Id = startWorkerNo + i;
                workers.Add(worker);
            }

            return workers;
        }

        private List<string> PerformMap(FileUri mapFuncFileName, IEnumerable<FileUri> filesToProcess)
        {
            var keys = new List<string>();

            var index = 0;
            foreach (var file in filesToProcess)
            {
                var worker = this.mapWorkers[(index++) % this.mapWorkers.Count];
                worker.Start(BluepathStorage.GetFileNameStatic(file.Uri), BluepathStorage.GetFileNameStatic(mapFuncFileName.Uri));
            }

            for (var i = 0; i < this.mapWorkers.Count; i++)
            {
                this.mapWorkers[i].Join();
            }

            for (var i = 0; i < this.mapWorkers.Count; i++)
            {
                keys.AddRange((IEnumerable<string>)this.mapWorkers[i].Result);
            }

            return keys;
        }

        private void PerformReduce(FileUri reduceFuncFileName, Dictionary<string, int> assignments)
        {
            var index = 0;
            foreach (var assignment in assignments)
            {
                var worker = this.reduceWorkers[assignment.Value];
                worker.Start(Base64.Encode(assignment.Key), BluepathStorage.GetFileNameStatic(reduceFuncFileName.Uri));
            }

            for (var i = 0; i < this.reduceWorkers.Count; i++)
            {
                this.reduceWorkers[i].Join();
            }
        }

        private Dictionary<string, int> TransferIntermediateFiles(IEnumerable<string> keys)
        {
            var reduceWorkersCount = this.reduceWorkers.Count;
            var result = new Dictionary<string, int>();

            var i = 0;
            foreach (var key in keys)
            {
                result.Add(key, (i++) % reduceWorkersCount);
            }

            return result;
        }
    }
}
