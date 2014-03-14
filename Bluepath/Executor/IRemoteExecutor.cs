namespace Bluepath.Executor
{
    using System;

    using Bluepath.ServiceReferences;

    public interface IRemoteExecutor : IExecutor
    {
        void Setup(IRemoteExecutorService remoteExecutorService, ServiceUri callbackUri);

        void Pulse(RemoteExecutorServiceResult result);
    }
}
