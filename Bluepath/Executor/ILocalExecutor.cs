namespace Bluepath.Executor
{
    using System;

    public interface ILocalExecutor : IExecutor
    {
        Exception Exception { get; }

        bool IsResultAvailable { get; }

        void InitializeNonGeneric(Func<object[], object> function, int? expectedNumberOfParameters = null, int? communicationObjectParameterIndex = null);
    }
}
