namespace Bluepath.Executor
{
    using System;
    using System.Reflection;

    public interface ILocalExecutor : IExecutor
    {
        /// <summary>
        /// Gets result which may be serialized in order to easily send it back.
        /// Useful for remote executor services.
        /// </summary>
        object SerializedResult { get; }

        /// <summary>
        /// Gets exception thrown during method execution.
        /// </summary>
        Exception Exception { get; }

        /// <summary>
        /// Gets a value indicating whether processing is stopped and result available.
        /// </summary>
        bool IsResultAvailable { get; }

        /// <summary>
        /// Initializes executor with non generic function (if generic function is used it must be wrapped).
        /// </summary>
        /// <param name="function">Static function/function wrapper that will be executed.</param>
        /// <param name="expectedNumberOfParameters">Number of parameters taken by function (if wrapped than pass number of wrapped function parameters).</param>
        /// <param name="communicationObjectParameterIndex">Which parameter is communication object.</param>
        /// <param name="parameters">Parameters information from wrapped function.</param>
        /// <param name="returnType">Return type of wrapped function.</param>
        void InitializeNonGeneric(
            Func<object[], object> function,
            int? expectedNumberOfParameters = null,
            int? communicationObjectParameterIndex = null,
            ParameterInfo[] parameters = null,
            Type returnType = null);
    }
}
