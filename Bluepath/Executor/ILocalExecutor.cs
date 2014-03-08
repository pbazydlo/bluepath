namespace Bluepath.Executor
{
    using System;
    using System.Threading;

    public interface ILocalExecutor : IExecutor
    {
        ThreadState ThreadState { get; }

        Exception Exception { get; }

        TimeSpan? ElapsedTime { get; }
    }
}
