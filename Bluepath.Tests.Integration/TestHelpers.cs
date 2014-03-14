namespace Bluepath.Tests.Integration
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

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
            
            var t = new Thread(
                () =>
                    {
                        var stream = process.StandardOutput.BaseStream;
                        using (var reader = new StreamReader(stream))
                        {
                            var line = default(string);
                            while ((line = reader.ReadLine()) != null)
                            {
                                Debug.WriteLine(string.Format("CONSOLE[{1}]> {0}", line, process.Id));
                            }

                            Debug.WriteLine(string.Format("CONSOLE[{0}]> (EOF)", process.Id));
                        }
                    });
            t.Start();

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
