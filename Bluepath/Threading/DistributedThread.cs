namespace Bluepath.Threading
{
    using System;

    using global::Bluepath.Executor;

    /// <summary>
    /// TODO: Description, Remote Execution, Choosing executing node
    /// </summary>
    public class DistributedThread
    {
        private ILocalExecutor executor;

        private Func<object[], object> function;

        private DistributedThread() { }

        public static DistributedThread Create(Func<object[], object> function)
        {
            return new DistributedThread()
            {
                function = function
            };
        }

        public void Start(object[] parameters)
        {
            // TODO: replace with RemoteExecutor
            this.executor = new LocalExecutor();
            this.executor.Initialize(this.function);
            this.executor.Execute(parameters);
        }

        public void Join()
        {
            this.executor.Join();
        }

        public object Result
        {
            get
            {
                return this.executor.Result;
            }
        }
    }
}
