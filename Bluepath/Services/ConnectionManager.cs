namespace Bluepath.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    public class ConnectionManager : IConnectionManager
    {
        private static readonly object DefaultLock = new object();

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1311:StaticReadonlyFieldsMustBeginWithUpperCaseLetter", Justification = "Public property starting with upper-case letter is exposed.")]
        // ReSharper disable once InconsistentNaming
        private static ConnectionManager defualt;

        private readonly List<ServiceReferences.IRemoteExecutorService> remoteServices;

        public ConnectionManager(ServiceReferences.IRemoteExecutorService remoteService, BluepathListener listener)
            : this(new List<ServiceReferences.IRemoteExecutorService>() { remoteService }, listener)
        {
        }

        public ConnectionManager(IEnumerable<ServiceReferences.IRemoteExecutorService> remoteServices, BluepathListener listener)
            : this(listener)
        {
            this.remoteServices.AddRange(remoteServices);
        }

        private ConnectionManager(BluepathListener listener)
        {
            this.remoteServices = new List<ServiceReferences.IRemoteExecutorService>();
            this.Listener = listener;
        }

        public static ConnectionManager Default
        {
            get
            {
                lock (ConnectionManager.DefaultLock)
                {
                    if (ConnectionManager.defualt == null)
                    {
                        if (BluepathSingleton.Instance.Listener == null)
                        {
                            throw new Exception("Can't create default connection manager. Initialize BluepathSingleton.Instance.Listener first.");
                        }

                        ConnectionManager.defualt = new ConnectionManager(BluepathSingleton.Instance.Listener);
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

        public BluepathListener Listener { get; private set; }
    }
}
