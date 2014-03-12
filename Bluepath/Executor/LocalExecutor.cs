﻿namespace Bluepath.Executor
{
    using System;
    using System.Threading;

    public class LocalExecutor : ILocalExecutor
    {
        private readonly object finishedRunningLock = new object();
        private object result;
        private Thread thread;
        private Func<object[], object> function;
        private bool finishedRunning;
        private DateTime? timeStarted;
        private DateTime? timeStopped;

        public LocalExecutor()
        {
            this.Eid = Guid.NewGuid();
            Log.TraceMessage("Local executor created.", keywords: this.Eid.AsLogKeywords("eid"));
        }

        public TimeSpan? ElapsedTime
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
        }

        public Guid Eid { get; private set; }

        public ExecutorState ExecutorState
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

        public object Result
        {
            get
            {
                lock (this.finishedRunningLock)
                {
                    if (this.finishedRunning)
                    {
                        return this.result;
                    }

                    throw new NullReferenceException("Cannot fetch results before starting and finishing Execute.");
                }
            }
        }

        public Exception Exception { get; private set; }

        public void Execute(object[] parameters)
        {
            lock (this.finishedRunningLock)
            {
                this.finishedRunning = false;
            }

            this.thread = new Thread(() =>
            {
                try
                {
                    // Run user code
                    this.result = this.function(parameters);
                }
                catch (Exception ex)
                {
                    // Handle exceptions that are caused by user code
                    this.Exception = ex;
                    Log.ExceptionMessage(ex, "Local executor caught exception in user code.", Log.MessageType.UserCodeException, this.Eid.AsLogKeywords("eid"));
                }

                lock (this.finishedRunningLock)
                {
                    this.finishedRunning = true;
                    this.timeStopped = DateTime.Now;
                }
            });

            this.timeStarted = DateTime.Now;
            this.thread.Start();
        }

        public void Join()
        {
            this.thread.Join();
        }

        public object GetResult()
        {
            return this.Result;
        }

        public void Initialize(Func<object[], object> function)
        {
            this.function = function;
        }

        public void Dispose()
        {
            // TODO: Add executor dispose logic here
            Log.TraceMessage("Local executor is being disposed.", keywords: this.Eid.AsLogKeywords("eid"));
        }
    }
}
