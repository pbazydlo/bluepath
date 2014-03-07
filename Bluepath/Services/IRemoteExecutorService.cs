namespace Bluepath.Services
{
    using System.ServiceModel;

    [ServiceContract]
    public interface IRemoteExecutorService : Executor.IExecutor
    {
        [OperationContract]
        void Initialize(byte[] methodHandle);
    }
}
