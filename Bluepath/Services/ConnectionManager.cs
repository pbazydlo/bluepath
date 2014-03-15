namespace Bluepath.Services
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    public class ConnectionManager : IConnectionManager
    {
        private static readonly object DefaultLock = new object();

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1311:StaticReadonlyFieldsMustBeginWithUpperCaseLetter", Justification = "Public property starting with upper-case letter is exposed.")]
        // ReSharper disable once InconsistentNaming
        private static ConnectionManager defualt;

        private readonly List<ServiceReferences.IRemoteExecutorService> remoteServices;

        public ConnectionManager(ServiceReferences.IRemoteExecutorService remoteService)
            : this(new List<ServiceReferences.IRemoteExecutorService>() { remoteService })
        {
        }

        public ConnectionManager(IEnumerable<ServiceReferences.IRemoteExecutorService> remoteServices)
            : this()
        {
            this.remoteServices.AddRange(remoteServices);
        }

        private ConnectionManager()
        {
            this.remoteServices = new List<ServiceReferences.IRemoteExecutorService>();
        }

        public static ConnectionManager Default
        {
            get
            {
                lock (ConnectionManager.DefaultLock)
                {
                    if (ConnectionManager.defualt == null)
                    {
                        ConnectionManager.defualt = new ConnectionManager();
                    }
                }

                return ConnectionManager.defualt;
            }
        }

        public List<ServiceReferences.IRemoteExecutorService> RemoteServices
        {
            get
            {
                return this.remoteServices;
            }
        }
    }
}
