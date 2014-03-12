namespace Bluepath.Executor
{
    using System;

    public interface ILocalExecutor : IExecutor
    {
        void Initialize(Func<object[], object> function);
        Exception Exception { get; }
    }
}
