namespace Bluepath.Exceptions
{
    using System;

    public class MissingRemoteServiceReferenceException : Exception
    {
        public MissingRemoteServiceReferenceException(string message)
            : base(message)
        {
        }
    }
}
