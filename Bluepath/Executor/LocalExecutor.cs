namespace Bluepath.Executor
{
    using System;
    using System.Threading;

    public class LocalExecutor : ILocalExecutor
    {
        private object result;
        private Thread thread;
        private Func<object[], object> function;
        private bool finishedRunning;
        private object finishedRunningLock = new object();
        private DateTime? timeStarted;
        private DateTime? timeStopped;

        public LocalExecutor()
        {
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

        public void Execute(object[] parameters)
        {
            lock (this.finishedRunningLock)
            {
                this.finishedRunning = false;
            }

            this.thread = new Thread(() =>
            {
                this.result = this.function(parameters);
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

        public void Initialize(Func<object[], object> function)
        {
            this.function = function;
        }

        public ThreadState ThreadState
        {
            get
            {
                return this.thread != null ? this.thread.ThreadState : ThreadState.Unstarted;
            }
        }

        public Exception Exception { get; private set; }

        public void ReportException(Exception exception)
        {
            this.Exception = exception;

            lock (this.finishedRunningLock)
            {
                this.finishedRunning = true;
                this.timeStopped = DateTime.Now;
            }
        }
    }
}
