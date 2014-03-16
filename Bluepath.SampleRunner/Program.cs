namespace Bluepath.SampleRunner
{
    using System;

    public class Program
    {
        public static void Main(string[] args)
        {
            int port = -1;
            string ip = "127.0.0.1";
            if ((args.Length > 0 && !int.TryParse(args[0], out port)) || args.Length == 0)
            {
                BluepathSingleton.Instance.Initialize(ip);
            }

            BluepathSingleton.Instance.Initialize(ip, port);

            Console.WriteLine("Press <Enter> to stop the service.");
            Console.ReadLine();

            BluepathSingleton.Instance.Listener.Stop();
        }
    }
}
