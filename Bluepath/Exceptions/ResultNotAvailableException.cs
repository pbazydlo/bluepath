namespace Bluepath.Exceptions
{
    using System;

    public class ResultNotAvailableException : Exception
    {
        public ResultNotAvailableException(string message)
            : base(message)
        {
        }
    }
}
