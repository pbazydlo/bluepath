namespace Bluepath.Exceptions
{
    using System;

    public class CannotInitializeDefaultConnectionManagerException : Exception
    {
        public CannotInitializeDefaultConnectionManagerException(string message)
            : base(message)
        {
        }
    }
}
