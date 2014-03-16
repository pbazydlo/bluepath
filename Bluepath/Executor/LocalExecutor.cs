namespace Bluepath.Executor
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Threading;

    using Bluepath.Exceptions;
    using Bluepath.Framework;

    public class LocalExecutor : Executor, ILocalExecutor
    {
        private readonly object finishedRunningLock = new object();
        private object result;
        private Thread thread;
        private Func<object[], object> function;
        private bool finishedRunning;
        private DateTime? timeStarted;
        private DateTime? timeStopped;
        private int? expectedNumberOfParameters;
        private int? communicationObjectParameterIndex;

        public LocalExecutor()
        {
            this.Eid = Guid.NewGuid();
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
                if (this.Exception != null)
                {
                    return ExecutorState.Faulted;
                }

                if (this.thread == null)
                {
                    return ExecutorState.NotStarted;
                }

                switch (this.thread.ThreadState)
                {
                    case ThreadState.Unstarted:
                        return ExecutorState.NotStarted;
                    case ThreadState.Stopped:
                    case ThreadState.Aborted:
                        return ExecutorState.Finished;
                    default:
                        return ExecutorState.Running;
                }
            }
        }

        public override object Result
        {
            get
            {
                lock (this.finishedRunningLock)
                {
                    if (!this.finishedRunning)
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
                    return this.finishedRunning;
                }
            }
        }

        public override void Execute(object[] parameters)
        {
            lock (this.finishedRunningLock)
            {
                this.finishedRunning = false;
            }

            if (parameters == null)
            {
                parameters = new object[0];
            }

            parameters = this.InjectCommunicationFrameworkObject(parameters);

            this.thread = new Thread(() =>
            {
                Log.TraceMessage("Local executor has started thread running user code.", Log.MessageType.UserTaskStateChanged, keywords: this.Eid.EidAsLogKeywords());

                try
                {
                    // Run user code
                    this.result = this.function(parameters);
                }
                catch (Exception ex)
                {
                    // Handle exceptions that are caused by user code
                    this.Exception = ex;
                    Log.ExceptionMessage(ex, "Local executor caught exception in user code.", Log.MessageType.UserCodeException | Log.MessageType.UserTaskStateChanged, this.Eid.EidAsLogKeywords());
                }

                lock (this.finishedRunningLock)
                {
                    this.finishedRunning = true;
                    this.timeStopped = DateTime.Now;
                }

                Log.TraceMessage("Local executor finished running user code.", Log.MessageType.UserTaskStateChanged, keywords: this.Eid.EidAsLogKeywords());
            });

            this.timeStarted = DateTime.Now;
            this.thread.Name = string.Format("User code runner thread on executor '{0}'", this.Eid);
            this.thread.Start();
        }

        public override void Join()
        {
            Log.TraceMessage("Local executor joins thread running user code...", keywords: this.Eid.EidAsLogKeywords());
            this.thread.Join();
        }

        public void Join(TimeSpan timeout)
        {
            var timer = new Timer(
                _ =>
                {
                    if (!this.finishedRunning)
                    {
                        this.thread.Abort();
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
