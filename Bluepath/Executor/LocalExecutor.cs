namespace Bluepath.Executor
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

        public ThreadState ThreadState
        {
            get
            {
                return this.thread != null ? this.thread.ThreadState : ThreadState.Unstarted;
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
    }
}
