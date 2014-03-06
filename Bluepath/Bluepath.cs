using Bluepath.Executor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath
{
    public class Bluepath
    {
        private static object bluepathSingletonLock = new object();
        private static Bluepath BluepathSingleton;

        private Bluepath()
        {

        }

        public static Bluepath Instance
        {
            get
            {
                lock (bluepathSingletonLock)
                {
                    if (BluepathSingleton == null)
                    {
                        BluepathSingleton = new Bluepath();
                    }

                    return BluepathSingleton;
                }
            }
        }

        public void Initialize(string ip)
        {
            var random = new Random();
            var port = random.Next(49152, 65535);

            var listenUri = string.Format("http://{0}:{1}/BluepathExecutorService.svc", ip, port);
            var callbackUri = listenUri;

            // if (callbackUri.Contains("0.0.0.0"))
            // {
            //    callbackUri = callbackUri.Replace("0.0.0.0", NetworkInfo.GetIpAddresses().First().Address.ToString());
            // }

            // Create the ServiceHost.
            using (ServiceHost host = new ServiceHost(typeof(RemoteExecutorService), new Uri(listenUri)))
            {
                Console.WriteLine("Worker URI is {0}", callbackUri);

                // Enable metadata publishing.
                ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                smb.HttpGetEnabled = true;
                smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                host.Description.Behaviors.Add(smb);
                host.Description.Behaviors.Find<ServiceDebugBehavior>().IncludeExceptionDetailInFaults = true;

                // Open the ServiceHost to start listening for messages. Since
                // no endpoints are explicitly configured, the runtime will create
                // one endpoint per base address for each service contract implemented
                // by the service.
                host.Open();

                Console.WriteLine("The service is ready at {0}", listenUri);
                Console.WriteLine("Press <Enter> to stop the service.");
                Console.ReadLine();

                // Close the ServiceHost.
                host.Close();
            }
        }
    }
}
