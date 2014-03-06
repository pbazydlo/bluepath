using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Executor
{
    public class RemoteExecutor : IExecutor
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
            throw new NotImplementedException();
        }

        public object Result
        {
            get { throw new NotImplementedException(); }
        }

        private Services.ExecutorClient client;
    }
}
