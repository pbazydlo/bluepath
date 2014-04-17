namespace Bluepath.Tests.Integration
{
    using System;
    using System.Collections.Concurrent;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    public static class TestHelpers
    {
        private const string BluepathServicePath = @"..\..\..\Bluepath.SampleRunner\bin\Debug\Bluepath.SampleRunner.exe";
        private const string RedisServicePath = @"..\..\..\packages\Redis-64.2.8.4\redis-server.exe";

        private static readonly ConcurrentDictionary<int, Process> SpawnedServices = new ConcurrentDictionary<int, Process>();

        //public static object RedisLock = new object();
        private static CleanUper cleanuper = new CleanUper();

        public static Process SpawnRemoteService(int port, ServiceType serviceType = ServiceType.Bluepath, int restartCounter = 3)
        {
            if (port != 0 && SpawnedServices.ContainsKey(port))
            {
                throw new Exception(string.Format("There is already service running on port '{0}'", port));
            }

            ProcessStartInfo processStartInfo = null;
            switch (serviceType)
            {
                case ServiceType.Bluepath:
                    processStartInfo = new ProcessStartInfo(TestHelpers.BluepathServicePath, port.ToString());
                    break;
                case ServiceType.Redis:
                    bool isKillFailure = true;
                    while(isKillFailure)
                    {
                        isKillFailure = false;
                        try
                        {
                            var redisInstances = Process.GetProcessesByName("redis-server");
                            foreach (var redisInstance in redisInstances)
                            {
                                return redisInstance;
                                //redisInstance.Kill();
                            }
                        }
                        catch(Win32Exception ex)
                        {
                            isKillFailure = true;
                            Debug.WriteLine(ex.ToString());
                            Thread.Sleep(100);
                        }
                    }

                    processStartInfo = new ProcessStartInfo(TestHelpers.RedisServicePath);
                    break;
            };

            processStartInfo.CreateNoWindow = true;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.UseShellExecute = false;
            var process = Process.Start(processStartInfo);
            bool isRedisStarted = false;
            bool isRedisError = false;

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
                            if (serviceType == ServiceType.Redis)
                            {
                                if (line.Contains("The server is now ready"))
                                {
                                    isRedisStarted = true;
                                }

                                if (line.Contains("system error"))
                                {
                                    isRedisError = true;
                                }
                            }

                            Debug.WriteLine(string.Format("CONSOLE[{1}]> {0}", line, process.Id));
                        }

                        Debug.WriteLine(string.Format("CONSOLE[{0}]> (EOF)", process.Id));
                    }
                });
            t.Start();

            Thread.Sleep(1000);
            if (serviceType == ServiceType.Redis)
            {
                while(!isRedisStarted)
                {
                    Thread.Sleep(100);
                    if(isRedisError)
                    {
                        break;
                    }
                }

                if(isRedisError)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch(Exception ex)
                    {
                        Debug.WriteLine(string.Format("Kill redis before restarting - exception: {0}", ex));
                    }

                    return SpawnRemoteService(port, serviceType, restartCounter - 1);
                }
            }

            return process;
        }

        public static void RepeatUntilTrue(Func<bool> function, int times = 5, TimeSpan? wait = null)
        {
            var waitTime = wait ?? new TimeSpan(days: 0, hours: 0, minutes: 0, seconds: 0, milliseconds: 500);
            int timesExecuted = 0;
            while (timesExecuted < times && !function())
            {
                System.Threading.Thread.Sleep(waitTime);
                timesExecuted++;
            }
        }

        public enum ServiceType
        {
            Bluepath,
            Redis
        }

        private class CleanUper : IDisposable
        {
            public void Dispose()
            {
                var redisInstances = Process.GetProcessesByName("redis-server");
                foreach (var redisInstance in redisInstances)
                {
                    redisInstance.Kill();
                }
            }
        }
    }
}
