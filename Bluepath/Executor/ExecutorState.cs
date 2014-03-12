namespace Bluepath.Executor
{
    public enum ExecutorState : int
    {
        /// <summary>
        /// Executor was created/initialized, but the processing has not started.
        /// </summary>
        NotStarted = 0,

        /// <summary>
        /// Executor is executing user code.
        /// </summary>
        Running = 1,

        /// <summary>
        /// Processing has finished and the result is available.
        /// </summary>
        Finished = 2,

        /// <summary>
        /// An exception occurred during execution.
        /// </summary>
        Faulted = 3
    }
}
