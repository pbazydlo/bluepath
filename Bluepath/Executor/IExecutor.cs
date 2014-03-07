using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Executor
{
    [ServiceContract]
    public interface IExecutor
    {
        [OperationContract]
        void Execute(object[] parameters);

        [OperationContract]
        void Join();

        [OperationContract]
        object GetResult();

        object Result { get; }
    }
}
