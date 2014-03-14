namespace Bluepath.Executor
{
    using System;
    using System.Reflection;

    using Bluepath.Exceptions;

    public abstract class Executor : IExecutor
    {
        public Guid Eid { get; protected set; }

        public ExecutorState ExecutorState { get; protected set; }

        public abstract TimeSpan? ElapsedTime { get; protected set; }

        public abstract object Result { get; }

        public abstract void Execute(object[] parameters);

        public abstract void Join();

        public object GetResult()
        {
            return this.Result;
        }

        public void Initialize<TFunc>(TFunc function)
        {
            var @delegate = function as Delegate;

            if (@delegate != null)
            {
                // function is Delegate
                this.InitializeFromMethod(@delegate.Method);
            }
            else
            {
                throw new DelegateExpectedException(function != null ? function.GetType() : null);
            }
        }

        public void Initialize<TResult>(Func<TResult> function)
        {
            this.InitializeFromMethod(function.Method);
        }

        public void Initialize<T1, TResult>(Func<T1, TResult> function)
        {
            this.InitializeFromMethod(function.Method);
        }

        public void Initialize<T1, T2, TResult>(Func<T1, T2, TResult> function)
        {
            this.InitializeFromMethod(function.Method);
        }

        public void Initialize<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> function)
        {
            this.InitializeFromMethod(function.Method);
        }

        public void Initialize<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> function)
        {
            this.InitializeFromMethod(function.Method);
        }

        public void Initialize<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> function)
        {
            this.InitializeFromMethod(function.Method);
        }

        public void Initialize<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> function)
        {
            this.InitializeFromMethod(function.Method);
        }

        public void Initialize<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> function)
        {
            this.InitializeFromMethod(function.Method);
        }

        public void Initialize<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> function)
        {
            this.InitializeFromMethod(function.Method);
        }

        protected abstract void InitializeFromMethod(MethodBase method);

        public abstract void Dispose();
    }
}
