namespace Bluepath.Executor
{
    using System.ServiceModel;

    [ServiceContract]
    public interface IExecutor
    {
        object Result { get; }

        [OperationContract]
        void Execute(object[] parameters);

        [OperationContract]
        void Join();

        [OperationContract]
        object GetResult();
    }
}
