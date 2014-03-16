namespace Bluepath.Services
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Description;

    public class BluepathListener : IListener
    {
        private static readonly object DefaultPropertyLock = new object();
        private static BluepathListener defaultListener;
        private readonly ServiceHost host;
        private readonly object consoleThreadLock = new object();
        private System.Threading.Thread consoleThread;

        public BluepathListener(string ip, int? port = null)
        {
            lock (this.consoleThreadLock)
            {
                // do not allow multiple initialize
                if (this.consoleThread != null)
                {
                    return;
                }

                var random = new Random();
                var randomPort = random.Next(49152, 65535);

                var listenUri = string.Format("http://{0}:{1}/BluepathExecutorService.svc", ip, port ?? randomPort);
                var callbackUri = listenUri;

                ////if (callbackUri.Contains("0.0.0.0"))
                ////{
                ////    callbackUri = callbackUri.Replace("0.0.0.0", NetworkInfo.GetIpAddresses().First().Address.ToString());
                ////}

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

                this.consoleThread = new System.Threading.Thread(() =>
                {
                    lock (this.consoleThreadLock)
                    {
                        Console.WriteLine("The service is ready at {0}", listenUri);
                        Console.WriteLine("Binding {0}", this.host.Description.Endpoints[0].Binding.GetType().FullName);
                        Console.WriteLine("Press <Enter> to stop the service.");
                        Console.ReadLine();
                        System.Threading.Monitor.PulseAll(this.consoleThreadLock);
                        this.host.Close();
                    }
                });

                this.consoleThread.Start();
            }
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

        public void Wait()
        {
            lock (this.consoleThreadLock)
            {
                System.Threading.Monitor.Wait(this.consoleThreadLock);
            }
        }

        public void Stop()
        {
            lock (this.consoleThreadLock)
            {
                if (this.consoleThread == null)
                {
                    return;
                }

                this.consoleThread.Abort();
                this.consoleThread = null;

                if (BluepathListener.Default == this)
                {
                    BluepathListener.Default = null;
                }

                System.Threading.Monitor.PulseAll(this.consoleThreadLock);
            }

            // Close the ServiceHost.
            this.host.Close();
        }
    }
}
