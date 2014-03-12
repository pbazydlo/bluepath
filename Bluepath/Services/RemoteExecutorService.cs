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
        /// Gets executor with given id.
        /// </summary>
        /// <param name="eid">Identifier of the executor.</param>
        /// <returns>Local executor.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if executor with given id doesn't exist.</exception>
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
            }
            while (!getSuccess);

            return executor;
        }

        /// <summary>
        /// Initializes remote executor.
        /// </summary>
        /// <param name="methodHandle">Serialized runtime method handle.</param>
        /// <returns>Identifier of the executor.</returns>
        /// <exception cref="ArgumentException">Thrown if method pointed by the handle is not static.</exception>
        public Guid Initialize(byte[] methodHandle)
        {
            var methodFromHandle = methodHandle.DeserializeMethodHandle();
            if (!methodFromHandle.IsStatic)
            {
                throw new ArgumentException("Executor supports only static methods.", "methodHandle");
            }

            ILocalExecutor executor;
            do
            {
                executor = new LocalExecutor();
            }
            while (!Executors.TryAdd(executor.Eid, executor));

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
        /// <param name="callbackUri">Specifies uri which will be used for callback.
        /// Null for no callback.</param>
        public void Execute(Guid eid, object[] parameters, ServiceUri callbackUri)
        {
            var executor = GetExecutor(eid);
            executor.Execute(parameters);

            if (callbackUri != null)
            {
                new Thread(() =>
                {
                    using (var client =
                        new Bluepath.ServiceReferences.RemoteExecutorServiceClient(
                            ServiceUri.ServiceBinding,
                            callbackUri.Address.ToEndpointAddress()))
                    {
                        // Join on local executor doesn't throw exceptions by design
                        // Exception caused by user code (if any) can be accessed using Exception property
                        executor.Join();

                        var result = new RemoteExecutorServiceResult
                        {
                            ElapsedTime = executor.ElapsedTime,
                            ExecutorState = executor.ExecutorState,
                            Error = executor.Exception
                        };

                        // TODO: client.ExecuteCallback(eid, result);
                    }
                }).Start();
            }
        }

        /// <summary>
        /// Returns current processing state or result after completion. 
        /// To avoid polling pass callback URI when invoking Execute method.
        /// </summary>
        /// <param name="eid">Identifier of executor.</param>
        /// <returns>Remote executor state.</returns>
        public RemoteExecutorServiceResult TryJoin(Guid eid)
        {
            var executor = GetExecutor(eid);
            var result = new RemoteExecutorServiceResult
                             {
                                 ElapsedTime = executor.ElapsedTime,
                                 ExecutorState = executor.ExecutorState
                             };

            switch (executor.ExecutorState)
            {
                case ExecutorState.Finished:
                    result.Result = executor.Result;

                    // we have to make sure that the message with the result is not lost
                    DisposeExecutor(executor);
                    break;
                case ExecutorState.Faulted:
                    result.Error = executor.Exception;

                    DisposeExecutor(executor);
                    break;
            }

            return result;
        }

        private static void DisposeExecutor(ILocalExecutor executor)
        {
            var eid = executor.Eid;

            if (executor.ExecutorState == ExecutorState.Running)
            {
                throw new Exception("Can't dispose running executor.");
            }

            executor.Dispose();

            var removed = false;
            do
            {
                if (!Executors.ContainsKey(eid))
                {
                    break;
                }

                removed = Executors.TryRemove(eid, out executor);
            }
            while (!removed);
        }
    }
}
