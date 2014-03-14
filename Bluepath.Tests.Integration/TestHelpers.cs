namespace Bluepath.Tests.Integration
{
    using System.Collections.Generic;
    using System.Diagnostics;

    public static class TestHelpers
    {
        private static readonly string RemoteServicePath = @"..\..\..\Bluepath.SampleRunner\bin\Debug\Bluepath.SampleRunner.exe";
        private static readonly List<Process> SpawnedServices = new List<Process>();
        public static bool SpawnRemoteService(int port)
        {
            var processStartInfo = new ProcessStartInfo(TestHelpers.RemoteServicePath, port.ToString());
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.UseShellExecute = false;
            var process = Process.Start(processStartInfo);
            SpawnedServices.Add(process);
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
