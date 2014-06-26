namespace Bluepath.Executor
{
    using System;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using Bluepath.Exceptions;
    using Bluepath.Extensions;
    using Bluepath.Framework;

    public class LocalExecutor : Executor, ILocalExecutor
    {
        private readonly object finishedRunningLock = new object();
        private readonly ManualResetEvent doneEvent = new ManualResetEvent(false);
        private object result;
        private Func<object[], object> function;
        private ExecutorState executorState;
        private DateTime? timeStarted;
        private DateTime? timeStopped;
        private int? expectedNumberOfParameters;
        private int? communicationObjectParameterIndex;
        private bool[] parameterInfos;
        private bool serializeResult = false;

        public LocalExecutor()
        {
            this.Eid = Guid.NewGuid();
            this.executorState = ExecutorState.NotStarted;
            Log.TraceMessage(Log.Activity.Local_executor_created, "Local executor created.", keywords: this.Eid.EidAsLogKeywords());
        }

        public override TimeSpan? ElapsedTime
        {
            get
            {
                if (this.timeStarted == null)
                {
                    return null;
                }

                if (this.timeStopped == null)
                {
                    return DateTime.Now - this.timeStarted;
                }

                return this.timeStopped - this.timeStarted;
            }

            protected set
            {
                throw new NotSupportedException("Elapsed time on local executor is computed and cannot be set.");
            }
        }

        public override ExecutorState ExecutorState
        {
            get
            {
                return this.executorState;
            }
        }

        public override object Result
        {
            get
            {
                lock (this.finishedRunningLock)
                {
                    if (this.executorState != ExecutorState.Finished)
                    {
                        throw new ResultNotAvailableException("Cannot fetch results before starting and finishing Execute.");
                    }

                    Log.TraceMessage(Log.Activity.Local_executor_returns_processing_result,"Local executor returns processing result.", keywords: this.Eid.EidAsLogKeywords());
                    return this.result;
                }
            }
        }

        public object SerializedResult
        {
            get
            {
                var result = this.Result;
                if (this.serializeResult)
                {
                    return result.Serialize();
                }

                return result;
            }
        }

        public Exception Exception { get; private set; }

        public bool IsResultAvailable
        {
            get
            {
                lock (this.finishedRunningLock)
                {
                    return this.executorState == ExecutorState.Finished;
                }
            }
        }

        /// <summary>
        /// Starts thread execution.
        /// </summary>
        /// <param name="parameters">Parameters to pass to the method. Those to be injected can be ommited or should be null.</param>
        /// <exception cref="NotSupportedException">The common language runtime (CLR) is hosted, and the host does not support ThreadPool.QueueUserWorkItem action.</exception>
        public override void Execute(object[] parameters)
        {
            lock (this.finishedRunningLock)
            {
                this.executorState = ExecutorState.Running;
            }

            this.doneEvent.Reset();

            if (parameters == null)
            {
                parameters = new object[0];
            }

            parameters = this.InjectCommunicationFrameworkObject(parameters);
            if (this.parameterInfos != null)
            {
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (this.parameterInfos[i] && (parameters[i] is byte[]))
                    {
                        parameters[i] = ((byte[])parameters[i]).Deserialize<object>();
                    }
                }
            }

            var availableWorkerThreads = default(int);
            var availableCompletionPortThreads = default(int);
            var maxWorkerThreads = default(int);
            var maxCompletionPortThreads = default(int);
            ThreadPool.GetAvailableThreads(out availableWorkerThreads, out availableCompletionPortThreads);
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);

            Log.TraceMessage(Log.Activity.Queueing_user_task, string.Format("Queueing user task. Available worker threads: {0}/{1}, available completion port threads: {2}/{3}.", availableWorkerThreads, maxWorkerThreads, availableCompletionPortThreads, maxCompletionPortThreads), Log.MessageType.Trace, keywords: this.Eid.EidAsLogKeywords());

            ThreadPool.QueueUserWorkItem(
                (threadContext) =>
                {
                    Log.TraceMessage(
                        Log.Activity.Local_executor_started_running_user_code,
                        "Local executor has started thread running user code.",
                        Log.MessageType.UserTaskStateChanged,
                        keywords: this.Eid.EidAsLogKeywords());

                    try
                    {
                        // Run user code
                        this.result = this.function(parameters);
                    }
                    catch (Exception ex)
                    {
                        // Handle exceptions that are caused by user code
                        this.Exception = ex;
                        Log.ExceptionMessage(
                            ex,
                            Log.Activity.Local_executor_caught_exception_in_user_code,
                            "Local executor caught exception in user code.",
                            Log.MessageType.UserCodeException | Log.MessageType.UserTaskStateChanged,
                            this.Eid.EidAsLogKeywords());
                    }

                    lock (this.finishedRunningLock)
                    {
                        this.executorState = this.Exception == null ? ExecutorState.Finished : ExecutorState.Faulted;
                        this.timeStopped = DateTime.Now;
                    }

                    Log.TraceMessage(
                        Log.Activity.Local_executor_finished_running_user_code,
                        "Local executor finished running user code.",
                        Log.MessageType.UserTaskStateChanged,
                        keywords: this.Eid.EidAsLogKeywords());

                    this.doneEvent.Set();
                });

            this.timeStarted = DateTime.Now;
            // this.thread.Name = string.Format("User code runner thread on executor '{0}'", this.Eid);
            // this.thread.Start();
        }

        public override void Join()
        {
            Log.TraceMessage(Log.Activity.Local_executor_joins_thread_running_user_code, "Local executor joins thread running user code...", keywords: this.Eid.EidAsLogKeywords());
            this.doneEvent.WaitOne();
        }

        public bool Join(TimeSpan timeout)
        {
            // natural behaviour of join with timeout is to stop trying to join
            // after timeout have passed and NOT to abort thread
            return this.doneEvent.WaitOne(timeout);
        }

        public void InitializeNonGeneric(
            Func<object[], object> function,
            int? expectedNumberOfParameters = null,
            int? communicationObjectParameterIndex = null,
            ParameterInfo[] parameters = null,
            Type returnType = null)
        {
            this.function = function;
            this.expectedNumberOfParameters = expectedNumberOfParameters;
            this.communicationObjectParameterIndex = communicationObjectParameterIndex;
            if (parameters != null)
            {
                this.parameterInfos = new bool[parameters.Length];
                for (var i = 0; i < parameters.Length; i++)
                {
                    var parameterType = parameters[i].ParameterType;
                    this.parameterInfos[i] = parameterType.IsClass && parameterType.IsSerializable;
                }
            }

            if (returnType != null)
            {
                if (returnType.IsClass && returnType.IsSerializable)
                {
                    this.serializeResult = true;
                }
            }

            Log.TraceMessage(Log.Activity.Local_executor_initialized,"Local executor initialized.", keywords: this.Eid.EidAsLogKeywords());
        }

        public override void Dispose()
        {
            // TODO: Add executor dispose logic here
            Log.TraceMessage(Log.Activity.Local_executor_is_being_disposed,"Local executor is being disposed.", keywords: this.Eid.EidAsLogKeywords());
        }

        protected override void InitializeFromMethod(MethodBase method)
        {
            // Check if method expects IBluepathCommunicationFramework object
            var methodParameters = method.GetParameters();
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

            if (method is MethodInfo)
            {
                returnType = ((MethodInfo)method).ReturnType;
            }

            this.InitializeNonGeneric(
                (parameters) => method.Invoke(null, parameters),
                methodParameters.Length,
                parameterFound ? parameterIndex : null,
                methodParameters,
                returnType);
        }

        /// <summary>
        /// Adds communication framework object if required by executed function.
        /// </summary>
        /// <param name="parameters">Initial parameters passed to function.</param>
        /// <returns>Parameters with added communication framework.</returns>
        private object[] InjectCommunicationFrameworkObject(object[] parameters)
        {
            if (!this.communicationObjectParameterIndex.HasValue || !this.expectedNumberOfParameters.HasValue)
            {
                return parameters;
            }

            var bluepathCommunicationFrameworkObject = new BluepathCommunicationFramework(this);

            if (parameters.Length == this.expectedNumberOfParameters.Value)
            {
                parameters[this.communicationObjectParameterIndex.Value] = bluepathCommunicationFrameworkObject;
            }
            else if (parameters.Length == this.expectedNumberOfParameters.Value - 1)
            {
                var parametersBeforeCommunicationObject = parameters.Take(this.communicationObjectParameterIndex.Value);
                var parametersAfterCommunicationObject = parameters.Skip(this.communicationObjectParameterIndex.Value).Take(this.expectedNumberOfParameters.Value - this.communicationObjectParameterIndex.Value - 1);

                parameters = parametersBeforeCommunicationObject.Union(new object[] { bluepathCommunicationFrameworkObject }).Union(parametersAfterCommunicationObject).ToArray();
            }

            return parameters;
        }
    }
}
