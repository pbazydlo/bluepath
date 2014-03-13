using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Extensions
{
    public static class RemoteExecutorServiceResultExtensions
    {
        public static ServiceReferences.RemoteExecutorServiceResult Convert(this Services.RemoteExecutorServiceResult result)
        {
            var res = new ServiceReferences.RemoteExecutorServiceResult()
            {
                Result = result.Result,
                ExecutorState = (ServiceReferences.ExecutorState)((int)result.ExecutorState),
                Error = result.Error,
                ElapsedTime = result.ElapsedTime
            };

            return res;
        }

        public static Services.RemoteExecutorServiceResult Convert(this ServiceReferences.RemoteExecutorServiceResult result)
        {
            var res = new Services.RemoteExecutorServiceResult()
            {
                Result = result.Result,
                ExecutorState = (Executor.ExecutorState)((int)result.ExecutorState),
                Error = result.Error,
                ElapsedTime = result.ElapsedTime
            };

            return res;
        }
    }
}
