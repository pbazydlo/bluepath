namespace Bluepath.Executor
{
    using System;

    using Bluepath.ServiceReferences;

    public interface IRemoteExecutor : IExecutor
    {
        void Setup(IRemoteExecutorService remoteExecutorService, ServiceUri callbackUri);

        void Initialize<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function);

        void Pulse(RemoteExecutorServiceResult result);
    }
}
