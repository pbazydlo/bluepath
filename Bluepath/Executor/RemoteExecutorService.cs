using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bluepath.Executor
{
    public class RemoteExecutorService : IExecutor
    {
        public void Execute(object[] parameters)
        {
            throw new NotImplementedException();
        }

        public void Join()
        {
            throw new NotImplementedException();
        }

        public object GetResult()
        {
            return this.Result;
        }

        public object Result
        {
            get { throw new NotImplementedException(); }
        }

        private Thread executor;

        private Func<object[], object> function;

        
    }
}
