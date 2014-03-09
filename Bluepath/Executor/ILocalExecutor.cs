namespace Bluepath.Executor
{
    using System;

    public interface ILocalExecutor : IExecutor
    {
        Exception Exception { get; }
    }
}
