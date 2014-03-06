using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bluepath
{
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
            this.executor = new Thread(() =>
            {
                this.Result = this.function(parameters);
            });

            this.executor.Start();
        }

        public void Join()
        {
            this.executor.Join();
        }

        public object Result { get; private set; }

        private Thread executor;
        
        private Func<object[], object> function;

        private DistributedThread() { }
    }
}
