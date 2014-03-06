using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bluepath
{
    /// <summary>
    /// TODO: Description, Remote Execution, Choosing executing node
    /// </summary>
    public class DistributedThread
    {

        public static DistributedThread Create(Func<object[], object> function)
        {
            return new DistributedThread()
            {
                function = function
            };
        }

        public void Start(object[] parameters)
        {
            this.executor = new LocalExecutor(this.function);
            this.executor.Execute(parameters);
        }

        public void Join()
        {
            this.executor.Join();
        }

        public object Result
        {
            get
            {
                return this.executor.Result;
            }
        }

        private DistributedThread() { }

        private IExecutor executor;

        private Func<object[], object> function;
    }
}
