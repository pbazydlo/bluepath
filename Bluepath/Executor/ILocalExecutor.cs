using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bluepath.Executor
{
    public interface ILocalExecutor : IExecutor
    {
        ThreadState ThreadState { get; }

        Exception Exception { get; }

        TimeSpan? ElapsedTime { get; }
    }
}
