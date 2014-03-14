namespace Bluepath.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Threading;

    using Bluepath.Executor;
    using Bluepath.Extensions;
    using Bluepath.Framework;

    /// <summary>
    /// Represents endpoint, runs thread using local executor on the remote machine.
    /// </summary>
    public class RemoteExecutorService : IRemoteExecutorService
    {
        // TODO: When should we remove executors (both local and remote), after Join/Callback?
        // we shouldn't use another lock for ConcurrentDictionary access http://arbel.net/2013/02/03/best-practices-for-using-concurrentdictionary/
        // private static readonly object ExecutorsLock = new object();
        private static readonly ConcurrentDictionary<Guid, ILocalExecutor> Executors = new ConcurrentDictionary<Guid, ILocalExecutor>();
        private static readonly ConcurrentDictionary<Guid, IRemoteExecutor> RemoteExecutors = new ConcurrentDictionary<Guid, IRemoteExecutor>();

        /// <summary>
        /// Gets local executor with given id.
        /// </summary>
        /// <param name="eId">Identifier of the executor.</param>
        /// <returns>Local executor.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if executor with given id doesn't exist.</exception>
        public static ILocalExecutor GetExecutor(Guid eId)
        {
            ILocalExecutor executor;
            var getSuccess = false;
            do
            {
                if (!Executors.ContainsKey(eId))
                {
                    throw new ArgumentOutOfRangeException("eId", string.Format("Executor with eId '{0}' doesn't exist.", eId));
                }

                getSuccess = Executors.TryGetValue(eId, out executor);
            }
            while (!getSuccess);

            return executor;
        }

        /// <summary>
        /// Gets remote executor with given id.
        /// </summary>
        /// <param name="eId">Executor identifier</param>
        /// <returns>Remote executor.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if executor with given id doesn't exist.</exception>
        public static IRemoteExecutor GetRemoteExecutor(Guid eId)
        {
            IRemoteExecutor executor;
            var getSuccess = false;
            do
            {
                if (!RemoteExecutors.ContainsKey(eId))
                {
                    throw new ArgumentOutOfRangeException("eId", string.Format("RemoteExecutor with eId '{0}' doesn't exist.", eId));
                }

                getSuccess = RemoteExecutors.TryGetValue(eId, out executor);
            }
            while (!getSuccess);

            return executor;
        }

        public static void RegisterRemoteExecutor(IRemoteExecutor executor)
        {
            if (executor.Eid == default(Guid))
            {
                throw new ArgumentException("executor", "Remote executor needs to be initialized first.");
            }

            do
            {
                // TODO: It would be good to have some way of checking that before calling Register...
                if (RemoteExecutors.ContainsKey(executor.Eid))
                {
                    throw new ArgumentException("executor", "Given remote executor already exists.");
                }
            }
            while (!RemoteExecutors.TryAdd(executor.Eid, executor));
        }

        /// <summary>
        /// Initializes remote executor.
        /// </summary>
        /// <param name="methodHandle">Serialized runtime method handle.</param>
        /// <returns>Identifier of the executor.</returns>
        /// <exception cref="ArgumentException">Thrown if method pointed by the handle is not static.</exception>
        public Guid Initialize(byte[] methodHandle)
        {
            try
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

                InitializeLocalExecutor(executor, methodFromHandle);

                return executor.Eid;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.ExceptionMessage(ex);
                throw ex;
            }
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

            Log.TraceMessage(
                string.Format(
                        "Starting local executor. Upon completion callback will{0} be sent{1}{2}.",
                        callbackUri != null ? string.Empty : " not",
                        callbackUri != null ? " to " : string.Empty,
                        callbackUri != null ? callbackUri.Address : string.Empty),
                        keywords: executor.Eid.EidAsLogKeywords());

            executor.Execute(parameters);

            if (callbackUri != null)
            {
                var t = new Thread(() =>
                {
                    using (var client =
                        new Bluepath.ServiceReferences.RemoteExecutorServiceClient(
                            callbackUri.Binding,
                            callbackUri.ToEndpointAddress()))
                    {
                        // Join on local executor doesn't throw exceptions by design
                        // Exception caused by user code (if any) can be accessed using Exception property
                        executor.Join();

                        Log.TraceMessage("Sending callback with result.", keywords: executor.Eid.EidAsLogKeywords());

                        var result = new ServiceReferences.RemoteExecutorServiceResult
                        {
                            Result = executor.IsResultAvailable ? executor.Result : null,
                            ElapsedTime = executor.ElapsedTime,
                            ExecutorState = (ServiceReferences.ExecutorState)((int)executor.ExecutorState),
                            Error = executor.Exception
                        };

                        client.ExecuteCallback(eid, result);
                    }
                });

                t.Name = string.Format("Execute callback sender thread for executor '{0}'", executor.Eid);
                t.Start();
            }
        }

        /// <summary>
        /// Called in response to Execute after processing has finished.
        /// </summary>
        /// <param name="eId">Executor identifier</param>
        /// <param name="executeResult">Executor processing result.</param>
        public void ExecuteCallback(Guid eId, RemoteExecutorServiceResult executeResult)
        {
            var executor = GetRemoteExecutor(eId);
            executor.Pulse(executeResult.Convert());
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

            // TODO: Should dispose on request?
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

        private static void InitializeLocalExecutor(ILocalExecutor executor, MethodBase methodFromHandle)
        {
            // Check if method expects IBluepathCommunicationFramework object
            var methodParameters = methodFromHandle.GetParameters();
            var communicationFrameworkObjectType = typeof(IBluepathCommunicationFramework);
            int? parameterIndex = -1;
            var parameterFound = false;

            foreach (var parameter in methodParameters)
            {
                parameterIndex++;
                if (parameter.ParameterType == communicationFrameworkObjectType)
                {
                    parameterFound = true;
                    break;
                }
            }

            executor.Initialize((parameters) => methodFromHandle.Invoke(null, parameters), methodParameters.Length, parameterFound ? parameterIndex : null);
        }
    }
}
