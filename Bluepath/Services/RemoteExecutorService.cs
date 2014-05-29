namespace Bluepath.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;

    using Bluepath.Executor;
    using Bluepath.Extensions;
    using Bluepath.Framework;
    using Bluepath.Exceptions;

    /// <summary>
    /// Represents endpoint, runs thread using local executor on the remote machine.
    /// </summary>
    public class RemoteExecutorService : IRemoteExecutorService
    {
        // TODO: When should we remove executors (both local and remote), after Join/Callback?
        // we shouldn't use another lock for ConcurrentDictionary access http://arbel.net/2013/02/03/best-practices-for-using-concurrentdictionary/
        // private static readonly object ExecutorsLock = new object();
        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<Guid, ILocalExecutor>> Executors = new ConcurrentDictionary<int, ConcurrentDictionary<Guid, ILocalExecutor>>();
        private static readonly ConcurrentDictionary<Guid, IRemoteExecutor> RemoteExecutors = new ConcurrentDictionary<Guid, IRemoteExecutor>();

        /// <summary>
        /// Gets local executor with given id.
        /// </summary>
        /// <param name="eid">Identifier of the executor.</param>
        /// <returns>Local executor.</returns>
        /// <exception cref="ExecutorDoesntExistException">Thrown if executor with given id doesn't exist.</exception>
        public static ILocalExecutor GetExecutor(Guid eid)
        {
            var sourcePort = RemoteExecutorService.GetPortNumberFromRequest();
            if (!Executors.ContainsKey(sourcePort))
            {
                throw new Exception(string.Format("There are no executors running on port '{0}'.", sourcePort));
            }

            ILocalExecutor executor;
            var getSuccess = false;
            do
            {
                if (!Executors[sourcePort].ContainsKey(eid))
                {
                    throw new ExecutorDoesntExistException("eid", string.Format("Executor with eid '{0}' doesn't exist.", eid));
                }

                getSuccess = Executors[sourcePort].TryGetValue(eid, out executor);
            }
            while (!getSuccess);

            return executor;
        }

        /// <summary>
        /// Gets remote executor with given id.
        /// </summary>
        /// <param name="eid">Executor identifier</param>
        /// <returns>Remote executor.</returns>
        /// <exception cref="ExecutorDoesntExistException">Thrown if executor with given id doesn't exist.</exception>
        public static IRemoteExecutor GetRemoteExecutor(Guid eid)
        {
            IRemoteExecutor executor;
            var getSuccess = false;
            do
            {
                if (!RemoteExecutors.ContainsKey(eid))
                {
                    throw new ExecutorDoesntExistException("eid", string.Format("RemoteExecutor with eid '{0}' doesn't exist.", eid));
                }

                getSuccess = RemoteExecutors.TryGetValue(eid, out executor);
            }
            while (!getSuccess);

            return executor;
        }

        public static void RegisterRemoteExecutor(IRemoteExecutor executor)
        {
            var sourcePort = RemoteExecutorService.GetPortNumberFromRequest();

            if (executor.Eid == default(Guid))
            {
                throw new ArgumentException("Remote executor needs to be initialized first.", "executor");
            }

            do
            {
                // TODO: It would be good to have some way of checking that before calling Register...
                if (RemoteExecutors.ContainsKey(executor.Eid))
                {
                    throw new ArgumentException("Given remote executor already exists.", "executor");
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

                var sourcePort = RemoteExecutorService.GetPortNumberFromRequest();
                if (!Executors.ContainsKey(sourcePort))
                {
                    Executors.TryAdd(sourcePort, new ConcurrentDictionary<Guid, ILocalExecutor>());
                }

                ILocalExecutor executor;
                do
                {
                    executor = new LocalExecutor();
                }
                while (!Executors[sourcePort].TryAdd(executor.Eid, executor));

                InitializeLocalExecutor(executor, methodFromHandle);

                return executor.Eid;
            }
            catch (Exception ex)
            {
                Log.ExceptionMessage(ex);
                throw;
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

                        Log.TraceMessage(string.Format("Sending callback with result. State: {0}. Elapsed time: {1}.", executor.ExecutorState, executor.ElapsedTime), keywords: executor.Eid.EidAsLogKeywords());

                        var result = new ServiceReferences.RemoteExecutorServiceResult
                        {
                            Result = executor.IsResultAvailable ? executor.Result : null,
                            ElapsedTime = executor.ElapsedTime,
                            ExecutorState = (ServiceReferences.ExecutorState)((int)executor.ExecutorState),
                            Error = executor.Exception
                        };

                        // TODO: Serialization of result can fail and we should do something about it (like send an error message back)
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
        /// <param name="eid">Executor identifier</param>
        /// <param name="executeResult">Executor processing result.</param>
        public void ExecuteCallback(Guid eid, RemoteExecutorServiceResult executeResult)
        {
            var executor = GetRemoteExecutor(eid);
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
                    result.Result = executor.SerializedResult;

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

        public PerformanceStatistics GetPerformanceStatistics()
        {
            var numberOfTasks = new Dictionary<ExecutorState, int>();

            foreach (ExecutorState state in Enum.GetValues(typeof(ExecutorState)))
            {
                numberOfTasks[state] = 0;
            }

            var sourcePort = RemoteExecutorService.GetPortNumberFromRequest();
            if (RemoteExecutorService.Executors.ContainsKey(sourcePort))
            {
                foreach (var executor in RemoteExecutorService.Executors[sourcePort])
                {
                    Log.TraceMessage(
                        string.Format("[PerfStat] Executor is in '{0}' state.", executor.Value.ExecutorState),
                        keywords: executor.Value.Eid.EidAsLogKeywords());
                    numberOfTasks[executor.Value.ExecutorState]++;
                }
            }

            return new PerformanceStatistics() { NumberOfTasks = numberOfTasks };
        }

        private static int GetPortNumberFromRequest()
        {
            var port = default(int);

            try
            {
                port = System.ServiceModel.OperationContext.Current.RequestContext.RequestMessage.Headers.To.Port;
            }
            catch (NullReferenceException)
            {
            }

            return port;
        }

        private static void DisposeExecutor(ILocalExecutor executor)
        {
            var sourcePort = RemoteExecutorService.GetPortNumberFromRequest();
            if (!Executors.ContainsKey(sourcePort))
            {
                throw new Exception(string.Format("There are no executors running on port '{0}'.", sourcePort));
            }

            var eid = executor.Eid;

            if (executor.ExecutorState == ExecutorState.Running)
            {
                throw new Exception("Can't dispose running executor.");
            }

            executor.Dispose();

            var removed = false;
            do
            {
                if (!Executors[sourcePort].ContainsKey(eid))
                {
                    break;
                }

                removed = Executors[sourcePort].TryRemove(eid, out executor);
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
            Type returnType = null;

            foreach (var parameter in methodParameters)
            {
                parameterIndex++;
                if (parameter.ParameterType == communicationFrameworkObjectType)
                {
                    parameterFound = true;
                    break;
                }
            }

            if (methodFromHandle is MethodInfo)
            {
                returnType = ((MethodInfo)methodFromHandle).ReturnType;
            }

            executor.InitializeNonGeneric(
                (parameters) => methodFromHandle.Invoke(null, parameters),
                methodParameters.Length,
                parameterFound ? parameterIndex : null,
                methodParameters,
                returnType);
        }
    }
}
