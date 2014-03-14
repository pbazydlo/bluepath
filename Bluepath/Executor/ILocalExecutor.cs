namespace Bluepath.Executor
{
    using System;

    public interface ILocalExecutor : IExecutor
    {
        Exception Exception { get; }

        bool IsResultAvailable { get; }

        void Initialize(Func<object[], object> function, int? expectedNumberOfParameters = null, int? communicationObjectParameterIndex = null);
    }
}
