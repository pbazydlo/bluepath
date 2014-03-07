using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bluepath.Executor
{
    public class LocalExecutor : IExecutor
    {
        public LocalExecutor(Func<object[], object> function)
        {
            this.function = function;
        }

        public void Execute(object[] parameters)
        {
            lock (this.finishedRunningLock)
            {
                this.finishedRunning = false;
            }

            this.executor = new Thread(() =>
            {
                this.result = this.function(parameters);
                lock (this.finishedRunningLock)
                {
                    this.finishedRunning = true;
                }
            });

            this.executor.Start();
        }
        
        public void Join()
        {
            this.executor.Join();
        }

        public object GetResult()
        {
            return this.Result;
        }
        
        public object Result
        {
            get
            {
                lock(this.finishedRunningLock)
                {
                    if(this.finishedRunning)
                    {
                        return this.result;
                    }

                    throw new NullReferenceException("Cannot fetch results before starting and finishing Execute.");
                }
            }
        }

        private object result;

        private Thread executor;

        private Func<object[], object> function;

        private bool finishedRunning;
        private object finishedRunningLock = new object();
    }
}
