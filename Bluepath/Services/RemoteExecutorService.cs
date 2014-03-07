namespace Bluepath.Services
{
    using System.Reflection;

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
        private static readonly ConcurrentDictionary<Guid, ILocalExecutor> executors = new ConcurrentDictionary<Guid, ILocalExecutor>();

        public Guid Initialize(byte[] methodHandle)
        {
            var methodFromHandle = default(MethodBase);
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                stream.Write(methodHandle, 0, methodHandle.Length);
                stream.Seek(0, SeekOrigin.Begin);
                var runtimeMethodHandle = (RuntimeMethodHandle)formatter.Deserialize(stream);
                methodFromHandle = MethodBase.GetMethodFromHandle(runtimeMethodHandle);
            }

            var executor = new LocalExecutor();
            var eId = Guid.NewGuid();
            // TODO: Invoke(null -> what about non-static functions?
            executor.Initialize((parameters) => methodFromHandle.Invoke(null, parameters));

            while (!executors.TryAdd(eId, executor))
            {
                eId = Guid.NewGuid();
            }

            return eId;
        }

        /// <summary>
        /// Executes method set previously by Initialize method.
        /// </summary>
        /// <param name="eId">Unique identifier of the executor.</param>
        /// <param name="parameters">Parameters for the method. Note that it gets interpreted as object1, object2, etc.
        /// So if method expects one argument of type object[], you need to wrap it with additional object[] 
        /// (object[] { object[] } - outer array indicates that method accepts one parameter, and inner is actual parameter).</param>
        public void Execute(Guid eId, object[] parameters)
        {
            var executor = GetExecutor(eId);
            executor.Execute(parameters);
        }

        // TODO: maybe we should rename this method to TryGetResult or sth.
        // and TryJoin shuold really only 'try join' and sometimes cause timeouts
        public RemoteExecutorServiceResult TryJoin(Guid eId)
        {
            var executor = GetExecutor(eId);
            var result = new RemoteExecutorServiceResult();

            result.ElapsedTime = executor.ElapsedTime;

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

        public static ILocalExecutor GetExecutor(Guid eId)
        {
            ILocalExecutor executor;
            lock (ExecutorsLock)
            {
                if (!executors.ContainsKey(eId))
                {
                    throw new ArgumentOutOfRangeException(string.Format("eId: {0} doesn't exist!", eId));
                }

                bool getSuccess = false;
                do
                {
                    getSuccess = executors.TryGetValue(eId, out executor);
                } while (!getSuccess);
            }
            return executor;
        }
    }
}
