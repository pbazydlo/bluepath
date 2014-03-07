namespace Bluepath.Services
{
    using System;
    using System.ServiceModel;

    [ServiceContract]
    public interface IRemoteExecutorService
    {
        [OperationContract]
        Guid Initialize(byte[] methodHandle);

        [OperationContract]
        void Execute(Guid eId, object[] parameters);

        [OperationContract]
        RemoteExecutorServiceResult TryJoin(Guid eId);
    }
}
