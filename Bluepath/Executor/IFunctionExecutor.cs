using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Executor
{
    public interface IFunctionExecutor : IExecutor
    {
        void Initialize(Func<object[], object> function);
    }
}
