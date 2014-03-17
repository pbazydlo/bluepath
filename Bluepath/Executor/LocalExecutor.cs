namespace Bluepath.Executor
{
    using System;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using Bluepath.Exceptions;
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

        public LocalExecutor()
        {
            this.Eid = Guid.NewGuid();
            this.executorState = ExecutorState.NotStarted;
            Log.TraceMessage("Local executor created.", keywords: this.Eid.EidAsLogKeywords());
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

                    Log.TraceMessage("Local executor returns processing result.", keywords: this.Eid.EidAsLogKeywords());
                    return this.result;
                }
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
        /// 
        /// </summary>
        /// <param name="parameters"></param>
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

            var availableWorkerThreads = default(int);
            var availableCompletionPortThreads = default(int);
            var maxWorkerThreads = default(int);
            var maxCompletionPortThreads = default(int);
            ThreadPool.GetAvailableThreads(out availableWorkerThreads, out availableCompletionPortThreads);
            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);

            Log.TraceMessage(string.Format("Queueing user task. Available worker threads: {0}/{1}, available completion port threads: {2}/{3}.", availableWorkerThreads, maxWorkerThreads, availableCompletionPortThreads, maxCompletionPortThreads), Log.MessageType.Trace, keywords: this.Eid.EidAsLogKeywords());

            ThreadPool.QueueUserWorkItem(
                (threadContext) =>
                    {
                        Log.TraceMessage(
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
            Log.TraceMessage("Local executor joins thread running user code...", keywords: this.Eid.EidAsLogKeywords());
            this.doneEvent.WaitOne();
        }

        public void Join(TimeSpan timeout)
        {
            throw new NotSupportedException();

            var timer = new Timer(
                _ =>
                {
                    if (this.executorState == ExecutorState.Running)
                    {
                        // this.thread.Abort();
                    }
                },
                null,
                timeout.Milliseconds,
                Timeout.Infinite);

            this.Join();
        }

        public void InitializeNonGeneric(Func<object[], object> function, int? expectedNumberOfParameters = null, int? communicationObjectParameterIndex = null)
        {
            this.function = function;
            this.expectedNumberOfParameters = expectedNumberOfParameters;
            this.communicationObjectParameterIndex = communicationObjectParameterIndex;
            Log.TraceMessage("Local executor initialized.", keywords: this.Eid.EidAsLogKeywords());
        }

        public override void Dispose()
        {
            // TODO: Add executor dispose logic here
            Log.TraceMessage("Local executor is being disposed.", keywords: this.Eid.EidAsLogKeywords());
        }

        protected override void InitializeFromMethod(MethodBase method)
        {
            this.InitializeNonGeneric((parameters) => method.Invoke(null, parameters));
        }

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
