namespace Bluepath.Exceptions
{
    using System;

    [Serializable]
    public class RemoteException : Exception
    {
        public RemoteException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
