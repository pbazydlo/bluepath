namespace Bluepath
{
    using System;
    using System.ServiceModel;
    using System.ServiceModel.Description;

    using Bluepath.Services;

    public class BluepathListener
    {
        private readonly ServiceHost host;
        private readonly object consoleThreadLock = new object();
        private System.Threading.Thread consoleThread;

        public BluepathListener(string ip, int? port = null, bool makeDefault = false)
        {
            lock (this.consoleThreadLock)
            {
                // do not allow multiple initialize
                if (this.consoleThread != null)
                {
                    return;
                }

                if (makeDefault && BluepathListener.Default != null)
                {
                    throw new Exception("Cannot initialize more than one default listener.");
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
                host.Description.Behaviors.Add(smb);
                host.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;

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
                        Console.WriteLine("Binding {0}", host.Description.Endpoints[0].Binding.GetType().FullName);
                        Console.WriteLine("Press <Enter> to stop the service.");
                        Console.ReadLine();
                        System.Threading.Monitor.PulseAll(this.consoleThreadLock);
                        this.host.Close();
                    }
                });

                this.consoleThread.Start();

                if (makeDefault)
                {
                    BluepathListener.Default = this;
                }
            }
        }

        // TODO: mutual exclusion using lock on set
        public static BluepathListener Default { get; private set; }

        public ServiceUri CallbackUri { get; private set; }

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
