namespace Bluepath.MapReduce
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Bluepath.Framework;
    using Bluepath.MapReduce.Core;
    using Bluepath.Threading;

    using NetReduce;
    using NetReduce.Core;

    public class Coordinator
    {
        private List<DistributedThread> mapWorkers;
        private List<DistributedThread> reduceWorkers;
        private List<string> keys;

        public void Start(int maxMapperNo, int maxReducerNo, FileUri mapFuncFileName, FileUri reduceFuncFileName, IEnumerable<FileUri> filesToProcess)
        {
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
                    var storage = new BluepathStorage(bluepath.Storage);

                    var mapProvider = Loader.Load<IMapProvider>(mapFileName, storage);
                    var mapper = new Mapper(filePath, mapProvider.Map, storage);
                    var mapResult = mapper.PerformMap();

                    var keys = mapResult.Select(r => r.Key).Distinct().OrderBy(k => k);
                    foreach (var res in mapResult)
                    {
                        bluepath.Storage.Store(res.Key, res.Value);
                    }

                    return keys;
                });

            return this.InitThread(startWorkerNo, workersCount, mapFunc);
        }

        private List<DistributedThread> InitReduceThreads(int startWorkerNo, int workersCount)
        {
            var mapFunc = new Func<string, string, IBluepathCommunicationFramework, IEnumerable<string>>((filePath, reduceFuncName, bluepath) =>
            {
                var storage = new BluepathStorage(bluepath.Storage);

                var mapProvider = Loader.Load<IReduceProvider>(reduceFuncName, storage);
                var mapper = new Reducer(filePath, mapProvider.Reduce, storage);
                var reduceResult = mapper.PerformReduce();

                bluepath.Storage.Store(reduceResult.Key, reduceResult.Value);

                return new List<string>() { reduceResult.Key };
            });

            return this.InitThread(startWorkerNo, workersCount, mapFunc);
        }

        private List<DistributedThread> InitThread(int startWorkerNo, int workersCount, Func<string, string, IBluepathCommunicationFramework, IEnumerable<string>> func)
        {
            var workers = new List<DistributedThread>();
            for (int i = 0; i < workersCount; i++)
            {
                var worker = DistributedThread.Create(func);

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
                worker.Start(file, mapFuncFileName);
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
                worker.Start(assignment.Key, reduceFuncFileName);
            }

            for (var i = 0; i < this.reduceWorkers.Count; i++)
            {
                this.reduceWorkers[i].Join();
            }
        }

        private Dictionary<string, int> TransferIntermediateFiles(IEnumerable<string> keys)
        {
            throw new NotImplementedException();
        }
    }
}
