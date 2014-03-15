namespace Bluepath.Services
{
    using System.Collections.Generic;

    public interface IConnectionManager
    {
        List<ServiceReferences.IRemoteExecutorService> RemoteServices { get; }

        BluepathListener Listener { get; }
    }
}
