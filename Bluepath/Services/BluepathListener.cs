namespace Bluepath.Services
{
    using System;
    using System.Linq;
    using System.ServiceModel;
    using System.ServiceModel.Description;

    using Bluepath.Security;

    public class BluepathListener : IListener
    {
        private static readonly object DefaultPropertyLock = new object();
        private static BluepathListener defaultListener;
        private readonly ServiceHost host;
        private static readonly Guid nodeGuid = Guid.NewGuid();

        public BluepathListener(string ip, int? port = null)
        {
            if (!UserAccountControlHelper.IsUserAdministrator)
            {
                Log.TraceMessage("This service requires administrative privileges. Exiting.", Log.MessageType.Fatal);
            }

            var random = new Random();
            port = port ?? random.Next(49152, 65535);

            var listenUri = string.Format("http://{0}:{1}/BluepathExecutorService.svc", ip, port);
            var callbackUri = listenUri;

            if (callbackUri.Contains("0.0.0.0"))
            {
                callbackUri = callbackUri.Replace("0.0.0.0", NetworkInfo.GetIpAddresses().First().Address.ToString());
            }

            // Create the ServiceHost.
            this.host = new ServiceHost(typeof(RemoteExecutorService), new Uri(listenUri));

            Console.WriteLine("Worker URI is {0}", callbackUri);

            // Enable metadata publishing.
            var smb = new ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
            smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
            this.host.Description.Behaviors.Add(smb);
            this.host.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;

            // Open the ServiceHost to start listening for messages. Since
            // no endpoints are explicitly configured, the runtime will create
            // one endpoint per base address for each service contract implemented
            // by the service.
            this.host.Open();

            this.CallbackUri = ServiceUri.FromEndpointAddress(new EndpointAddress(callbackUri), this.host.Description.Endpoints[0].Binding);

            Log.TraceMessage(string.Format("The service is ready at {0}.", listenUri), Log.MessageType.ServiceStarted);
            Log.TraceMessage(string.Format("First of service bindings is of type {0}.", this.host.Description.Endpoints[0].Binding.GetType().FullName), Log.MessageType.Trace);
            Log.TraceMessage(string.Format("Callback URI seems to be {0}.", this.CallbackUri.Address), Log.MessageType.Info);
        }

        public static BluepathListener Default
        {
            get
            {
                return BluepathListener.defaultListener;
            }

            private set
            {
                lock (BluepathListener.DefaultPropertyLock)
                {
                    if (BluepathListener.defaultListener != null && value != null)
                    {
                        throw new Exception("There is already one default listener defined.");
                    }

                    BluepathListener.defaultListener = value;
                }
            }
        }

        public static Guid NodeGuid
        {
            get
            {
                return BluepathListener.nodeGuid;
            }
        }

        public ServiceUri CallbackUri { get; private set; }

        public static BluepathListener InitializeDefaultListener(string ip, int? port = null)
        {
            if (BluepathListener.defaultListener != null)
            {
                throw new Exception("There is already one default listener defined. Stop the current one before initializing new instance.");
            }

            var listener = new BluepathListener(ip, port);
            BluepathListener.Default = listener;
            return listener;
        }

        public void Stop()
        {
            // Close the ServiceHost.
            this.host.Close();

            if (BluepathListener.Default == this)
            {
                BluepathListener.Default = null;
            }
        }
    }
}
