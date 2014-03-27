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
        void Execute(Guid eId, object[] parameters, ServiceUri callbackUri);

        [OperationContract]
        void ExecuteCallback(Guid eid, RemoteExecutorServiceResult executeResult);

        [OperationContract]
        RemoteExecutorServiceResult TryJoin(Guid eId);

        [OperationContract]
        PerformanceStatistics GetPerformanceStatistics();
    }
}
