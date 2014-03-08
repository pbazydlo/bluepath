namespace Bluepath.Executor
{
    using System;

    public interface IExecutor : IDisposable
    {
        Guid Eid { get; }

        object Result { get; }

        void Execute(object[] parameters);

        void Join();

        object GetResult();

        void Initialize(Func<object[], object> function);
    }
}
