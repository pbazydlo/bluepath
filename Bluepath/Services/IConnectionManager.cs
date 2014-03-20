namespace Bluepath.Services
{
    using System.Collections.Generic;

    public interface IConnectionManager
    {
        IEnumerable<ServiceReferences.IRemoteExecutorService> RemoteServices { get; }

        IListener Listener { get; }
    }
}
