using Bluepath.Security;
using Bluepath.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.CentralizedDiscovery
{
    public class CentralizedDiscoveryListener
    {
        private ServiceHost host;

        public CentralizedDiscoveryListener(string ip, int? port)
        {
            if (!UserAccountControlHelper.IsUserAdministrator)
            {
                Log.TraceMessage("This service requires administrative privileges. Exiting.", Log.MessageType.Fatal);
            }

            var random = new Random();
            port = port ?? random.Next(49152, 65535);

            var listenUri = string.Format("http://{0}:{1}/BluepathCentralizedDiscovery.svc", ip, port);
            var callbackUri = listenUri;

            if (callbackUri.Contains("0.0.0.0"))
            {
                callbackUri = callbackUri.Replace("0.0.0.0", NetworkInfo.GetIpAddresses().First().Address.ToString());
            }

            // Create the ServiceHost.
            this.host = new ServiceHost(typeof(CentralizedDiscoveryService), new Uri(listenUri));

            Console.WriteLine("Discovery master URI is {0}", callbackUri);

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

            Log.TraceMessage(string.Format("The service is ready at {0}.", listenUri), Log.MessageType.ServiceStarted);
            Log.TraceMessage(string.Format("First of service bindings is of type {0}.", this.host.Description.Endpoints[0].Binding.GetType().FullName), Log.MessageType.Trace);
        }

        public void Stop()
        {
            // Close the ServiceHost.
            this.host.Close();
        }
    }
}
