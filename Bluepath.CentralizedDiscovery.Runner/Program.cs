using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.CentralizedDiscovery.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                Bluepath.Log.RedisHost = options.RedisConf;
                var listener = new Bluepath.CentralizedDiscovery.CentralizedDiscoveryListener(options.Ip, options.Port);
                Console.WriteLine("MasterURI: {0}", listener.MasterUri);
                Console.WriteLine("Press ENTER to stop");
                while (Console.ReadKey().Key != ConsoleKey.Enter) ;
            }
        }
    }
}
