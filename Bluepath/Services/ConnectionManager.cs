namespace Bluepath.Services
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;

    public class ConnectionManager : IConnectionManager
    {
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1311:StaticReadonlyFieldsMustBeginWithUpperCaseLetter", Justification = "Public property starting with upper-case letter is exposed.")]
        // ReSharper disable once InconsistentNaming
        private static readonly List<ServiceReferences.IRemoteExecutorService> sharedRemoteServices = new List<ServiceReferences.IRemoteExecutorService>();

        private readonly List<ServiceReferences.IRemoteExecutorService> remoteServices;

        public ConnectionManager()
        {
            this.remoteServices = ConnectionManager.SharedRemoteServices;
        }

        public ConnectionManager(ServiceReferences.IRemoteExecutorService remoteService)
            : this(new List<ServiceReferences.IRemoteExecutorService>() { remoteService })
        {
        }

        public ConnectionManager(IEnumerable<ServiceReferences.IRemoteExecutorService> remoteServices)
        {
            this.remoteServices = new List<ServiceReferences.IRemoteExecutorService>();
            this.remoteServices.AddRange(remoteServices);
        }

        public static List<ServiceReferences.IRemoteExecutorService> SharedRemoteServices
        {
            get
            {
                return sharedRemoteServices;
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
