namespace Bluepath.Executor
{
    using Bluepath.ServiceReferences;
    using System;

    public interface IRemoteExecutor : IExecutor
    {
        void Initialize<TResult>(IRemoteExecutorService remoteExecutorService, Func<TResult> function);

        void Initialize<T1, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, TResult> function);

        void Initialize<T1, T2, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, T2, TResult> function);

        void Initialize<T1, T2, T3, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, T2, T3, TResult> function);

        void Initialize<T1, T2, T3, T4, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, T2, T3, T4, TResult> function);

        void Initialize<T1, T2, T3, T4, T5, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, T2, T3, T4, T5, TResult> function);

        void Initialize<T1, T2, T3, T4, T5, T6, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, T2, T3, T4, T5, T6, TResult> function);

        void Initialize<T1, T2, T3, T4, T5, T6, T7, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, T2, T3, T4, T5, T6, T7, TResult> function);

        void Initialize<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(IRemoteExecutorService remoteExecutorService, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function);

        void Pulse(RemoteExecutorServiceResult result);
    }
}
