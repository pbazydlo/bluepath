namespace Bluepath.Exceptions
{
    using System;

    public class DelegateExpectedException : Exception
    {
        public DelegateExpectedException(Type type)
            : base(string.Format("Delegate was expected (got '{0}').", type))
        {
        }
    }
}
