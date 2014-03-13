using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Tests.Acceptance
{
    public static class TestHelpers
    {
        private static readonly string RemoteServicePath = @"..\..\..\Bluepath.SampleRunner\bin\Debug\Bluepath.SampleRunner.exe";
        private static readonly List<Process> SpawnedServices = new List<Process>();
        public static bool SpawnRemoteService(int port)
        {
            var process = Process.Start(new ProcessStartInfo(TestHelpers.RemoteServicePath, port.ToString()));
            System.Threading.Thread.Sleep(1000);
            return true;
        }

        public static void KillAllServices()
        {
            foreach (var service in TestHelpers.SpawnedServices)
            {
                service.Kill();
            }

            TestHelpers.SpawnedServices.Clear();
        }
    }
}
