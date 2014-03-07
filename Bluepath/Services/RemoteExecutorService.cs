namespace Bluepath.Services
{
    using Executor;
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading;

    /// <summary>
    /// Represents endpoint, runs thread using local executor on the remote machine
    /// </summary>
    public class RemoteExecutorService : IRemoteExecutorService
    {
        private static object ExecutorsLock = new object();
        private static ConcurrentDictionary<Guid, ILocalExecutor> Executors = new ConcurrentDictionary<Guid, ILocalExecutor>();

        Guid Initialize(byte[] methodHandle)
        {
            BinaryFormatter frm = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                stream.Write(methodHandle, 0, methodHandle.Length);
                stream.Seek(0, SeekOrigin.Begin);
                var mh = (RuntimeMethodHandle)frm.Deserialize(stream);
                var mb = System.Reflection.MethodInfo.GetMethodFromHandle(mh);
                var executor = new LocalExecutor();

                // TODO: Invoke(null -> what about non-static functions?
                executor.Initialize((parameters) => mb.Invoke(null, parameters));
                var eId = Guid.NewGuid();
                while (!Executors.TryAdd(eId, executor))
                {
                    eId = Guid.NewGuid();
                }

                return eId;
            }
        }

        public void Execute(Guid eId, object[] parameters)
        {
            ILocalExecutor executor = GetExecutor(eId);
            executor.Execute(parameters);
        }

        public RemoteExecutorServiceResult TryJoin(Guid eId)
        {
            var executor = GetExecutor(eId);
            switch(executor.State)
            {
                case ThreadState.Unstarted:
                    return new RemoteExecutorServiceResult()
                    {
                        IsFinished = RemoteExecutorServiceResult.State.NotStarted
                    };

                case ThreadState.Stopped:
                    try
                    {
                        var result = executor.Result;
                        return new RemoteExecutorServiceResult()
                        {
                            Result = result,
                            IsFinished = RemoteExecutorServiceResult.State.Finished
                        };
                    }
                    catch (Exception ex)
                    {
                        return new RemoteExecutorServiceResult()
                        {
                            Error = ex,
                            IsFinished = RemoteExecutorServiceResult.State.Faulted
                        };
                    }
                    
                default:
                    return new RemoteExecutorServiceResult()
                    {
                        IsFinished = RemoteExecutorServiceResult.State.Running
                    };
            }
        }

        private static ILocalExecutor GetExecutor(Guid eId)
        {
            ILocalExecutor executor;
            lock (ExecutorsLock)
            {
                if (!Executors.ContainsKey(eId))
                {
                    throw new ArgumentOutOfRangeException(string.Format("eId: {0} doesn't exist!", eId));
                }

                bool getSuccess = false;
                do
                {
                    getSuccess = Executors.TryGetValue(eId, out executor);
                } while (!getSuccess);
            }
            return executor;
        }
    }
}
