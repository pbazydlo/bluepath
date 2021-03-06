﻿namespace Bluepath.Exceptions
{
    using System;

    [Serializable]
    public class RemoteJoinAbortedException : Exception
    {
        public RemoteJoinAbortedException(string message)
            : base(message)
        {
        }

        public RemoteJoinAbortedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
