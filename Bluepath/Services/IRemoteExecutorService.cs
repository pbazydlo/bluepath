namespace Bluepath.Services
{
    using System.ServiceModel;

    public interface IRemoteExecutorService : Executor.IExecutor
    {
        [OperationContract]
        void Initialize(byte[] methodHandle);
    }
}
