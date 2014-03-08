namespace Bluepath.Executor
{
    using System;

    public interface IRemoteExecutor : IExecutor
    {
        void Initialize<TResult>(Func<TResult> function);

        void Initialize<T1, TResult>(Func<T1, TResult> function);

        void Initialize<T1, T2, TResult>(Func<T1, T2, TResult> function);
    }
}
