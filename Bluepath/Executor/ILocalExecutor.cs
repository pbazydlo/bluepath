namespace Bluepath.Executor
{
    using System;
    using System.Threading;

    public interface ILocalExecutor : IExecutor
    {
        Exception Exception { get; }

        TimeSpan? ElapsedTime { get; }
    }
}
