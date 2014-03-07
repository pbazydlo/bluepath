﻿namespace Bluepath.Services
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

        public Guid Initialize(byte[] methodHandle)
        {
            BinaryFormatter frm = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                stream.Write(methodHandle, 0, methodHandle.Length);
                stream.Seek(0, SeekOrigin.Begin);
                var mh = (RuntimeMethodHandle)frm.Deserialize(stream);
                var mb = System.Reflection.MethodInfo.GetMethodFromHandle(mh);
                var executor = new LocalExecutor();

                
                var eId = Guid.NewGuid();
                while (!Executors.TryAdd(eId, executor))
                {
                    eId = Guid.NewGuid();
                }

                executor.Initialize(
                    (parameters) =>
                        {
                            object result = null;
                            try
                            {
                                // TODO: Invoke(null -> what about non-static functions?
                                result = mb.Invoke(null, parameters);
                            }
                            catch (Exception ex)
                            {
                                // Handle exceptions that are caused by user code
                                var currentExecutor = GetExecutor(eId);
                                currentExecutor.ReportException(ex);
                            }

                            return result;
                        });

                return eId;
            }
        }

        public void Execute(Guid eId, object[] parameters)
        {
            ILocalExecutor executor = GetExecutor(eId);
            executor.Execute(parameters);
        }

        // TODO: maybe we should rename this method to TryGetResult or sth.
        // and TryJoin shuold really only 'try join' and sometimes cause timeouts
        public RemoteExecutorServiceResult TryJoin(Guid eId)
        {
            var executor = GetExecutor(eId);
            var result = new RemoteExecutorServiceResult();

            if (executor.Exception != null)
            {
                result.ExecutorState = RemoteExecutorServiceResult.State.Faulted;
                result.Error = executor.Exception;
            }
            else
            {
                switch (executor.ThreadState)
                {
                    case ThreadState.Unstarted:
                        result.ExecutorState = RemoteExecutorServiceResult.State.NotStarted;
                        break;
                    case ThreadState.Stopped:
                    case ThreadState.Aborted:
                        result.Result = executor.Result;
                        result.ExecutorState = RemoteExecutorServiceResult.State.Finished;

                        // TODO: remove the executor from dictionary after the result has been retrieved?
                        break;
                    default:
                        result.ExecutorState = RemoteExecutorServiceResult.State.Running;
                        break;
                }
            }

            return result;
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
