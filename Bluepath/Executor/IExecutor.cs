namespace Bluepath.Executor
{
    public interface IExecutor
    {
        object Result { get; }

        void Execute(object[] parameters);

        void Join();

        object GetResult();

        void Initialize(Func<object[], object> function);
    }
}
