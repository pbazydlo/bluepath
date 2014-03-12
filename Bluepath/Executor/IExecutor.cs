namespace Bluepath.Executor
{
    using System;

    public interface IExecutor : IDisposable
    {
        Guid Eid { get; }

        ExecutorState ExecutorState { get; }

        TimeSpan? ElapsedTime { get; }

        object Result { get; }

        void Execute(object[] parameters);

        void Join();

        object GetResult();
    }
}
