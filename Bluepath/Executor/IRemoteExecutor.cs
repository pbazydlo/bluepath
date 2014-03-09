﻿namespace Bluepath.Executor
{
    using System;

    public interface IRemoteExecutor : IExecutor
    {
        void Initialize<TResult>(Func<TResult> function);

        void Initialize<T1, TResult>(Func<T1, TResult> function);

        void Initialize<T1, T2, TResult>(Func<T1, T2, TResult> function);

        void Initialize<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function);

        void Initialize<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function);

        void Initialize<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function);

        void Initialize<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> function);

        void Initialize<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> function);

        void Initialize<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function);
    }
}
