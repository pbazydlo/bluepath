namespace Bluepath.Tests.Integration
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    public static class TestHelpers
    {
        private static readonly string RemoteServicePath = @"..\..\..\Bluepath.SampleRunner\bin\Debug\Bluepath.SampleRunner.exe";
        private static readonly ConcurrentDictionary<int, Process> SpawnedServices = new ConcurrentDictionary<int, Process>();
        public static bool SpawnRemoteService(int port)
        {
            if (SpawnedServices.ContainsKey(port))
            {
                throw new Exception(string.Format("There is already service running on port '{0}'", port));
            }

            var processStartInfo = new ProcessStartInfo(TestHelpers.RemoteServicePath, port.ToString());
            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.UseShellExecute = false;
            var process = Process.Start(processStartInfo);

            SpawnedServices.TryAdd(port, process);

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
            
            System.Threading.Thread.Sleep(1000);
            return true;
        }

        public static void KillAllServices()
        {
            foreach (var service in TestHelpers.SpawnedServices)
            {
                service.Value.Kill();
            }

            TestHelpers.SpawnedServices.Clear();
        }

        public static void KillService(int port)
        {
            Process process;
            if (TestHelpers.SpawnedServices.TryRemove(port, out process))
            {
                process.Kill();
            }
        }
    }
}
