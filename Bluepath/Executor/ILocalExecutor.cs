namespace Bluepath.Executor
{
    using System;
    using System.Reflection;

    public interface ILocalExecutor : IExecutor
    {
        object SerializedResult { get; }
        Exception Exception { get; }

        bool IsResultAvailable { get; }

        void InitializeNonGeneric(Func<object[], object> function, int? expectedNumberOfParameters = null,
            int? communicationObjectParameterIndex = null, ParameterInfo[] parameters = null, Type returnType = null);
    }
}
