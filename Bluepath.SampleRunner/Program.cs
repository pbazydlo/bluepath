namespace Bluepath.SampleRunner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            int port = -1;
            string ip = "127.0.0.1";
            if (args.Length > 0 && !int.TryParse(args[0], out port))
            {
                BluepathSingleton.Instance.Initialize(ip);
            }

            BluepathSingleton.Instance.Initialize(ip, port);
        }
    }
}
