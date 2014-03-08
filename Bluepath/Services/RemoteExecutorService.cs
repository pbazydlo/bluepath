namespace Bluepath.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    using global::Bluepath.Executor;

    using global::Bluepath.Extensions;

    /// <summary>
    /// Represents endpoint, runs thread using local executor on the remote machine.
    /// </summary>
    public class RemoteExecutorService : IRemoteExecutorService
    {
        // we shouldn't use another lock for ConcurrentDictionary access http://arbel.net/2013/02/03/best-practices-for-using-concurrentdictionary/
        // private static readonly object ExecutorsLock = new object();
        private static readonly ConcurrentDictionary<Guid, ILocalExecutor> Executors = new ConcurrentDictionary<Guid, ILocalExecutor>();

        /// <summary>
        /// Initializes remote executor.
        /// </summary>
        /// <param name="methodHandle">Serialized runtime method handle.</param>
        /// <returns>Identifier of the executor.</returns>
        /// <exception cref="ArgumentException">Thrown if method pointed by the handle is not static.</exception>
        public Guid Initialize(byte[] methodHandle)
        {
            ILocalExecutor executor;
            do
            {
                executor = new LocalExecutor();
            }
            while (!Executors.TryAdd(executor.Eid, executor));

            var methodFromHandle = methodHandle.DeserializeMethodHandle();
            if (!methodFromHandle.IsStatic)
            {
                throw new ArgumentException("Executor supports only static methods.", "methodHandle");
            }

            executor.Initialize((parameters) => methodFromHandle.Invoke(null, parameters));

            return executor.Eid;
        }

        /// <summary>
        /// Executes method set previously by Initialize method.
        /// </summary>
        /// <param name="eid">Unique identifier of the executor.</param>
        /// <param name="parameters">Parameters for the method. Note that it gets interpreted as object1, object2, etc.
        /// So if method expects one argument of type object[], you need to wrap it with additional object[] 
        /// (object[] { object[] } - outer array indicates that method accepts one parameter, and inner is actual parameter).</param>
        public void Execute(Guid eid, object[] parameters)
        {
            var executor = GetExecutor(eid);
            executor.Execute(parameters);
        }

        // TODO: maybe we should rename this method to TryGetResult or sth.
        // and TryJoin shuold really only 'try join' and sometimes cause timeouts
        public RemoteExecutorServiceResult TryJoin(Guid eid)
        {
            var executor = GetExecutor(eid);
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

                        // we have to make sure that the message with the result is not lost
                        DisposeExecutor(executor);
                        break;
                    default:
                        result.ExecutorState = RemoteExecutorServiceResult.State.Running;
                        break;
                }
            }

            return result;
        }

        public static ILocalExecutor GetExecutor(Guid eid)
        {
            ILocalExecutor executor;
            var getSuccess = false;
            do
            {
                if (!Executors.ContainsKey(eid))
                {
                    throw new ArgumentOutOfRangeException("eid", string.Format("Executor with eid '{0}' doesn't exist.", eid));
                }

                getSuccess = Executors.TryGetValue(eid, out executor);
            } while (!getSuccess);

            return executor;
        }

        private static void DisposeExecutor(ILocalExecutor executor)
        {
            var eid = executor.Eid;

            if (!(executor.ThreadState == ThreadState.Stopped
                || executor.ThreadState == ThreadState.Aborted
                || executor.ThreadState == ThreadState.Unstarted))
            {
                throw new Exception("Can't dispose running executor.");
            }

            var removed = false;
            do
            {
                if (!Executors.ContainsKey(eid))
                {
                    throw new ArgumentOutOfRangeException("eid", string.Format("Executor with eid '{0}' doesn't exist.", eid));
                }

                removed = Executors.TryRemove(eid, out executor);
            } while (!removed);

            // TODO: executor.Dispose?
        }
    }
}
