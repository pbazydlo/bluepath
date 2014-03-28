namespace Bluepath.Services
{
    using System.Collections.Generic;

    public interface IConnectionManager
    {
        IDictionary<ServiceUri, PerformanceStatistics> RemoteServices { get; }

        IListener Listener { get; }
    }
}
