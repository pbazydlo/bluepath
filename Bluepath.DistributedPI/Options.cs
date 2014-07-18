using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.DistributedPI
{
    class Options
    {
        [Option('i', "ip", Required = true)]
        public string Ip { get; set; }

        [Option('p', "port", Required = false)]
        public int? Port { get; set; }

        [Option('d', "discovery", Required = true)]
        public string CentralizedDiscoveryURI { get; set; }

        [Option('r', "redis", Required = true)]
        public string RedisHost { get; set; }

        [Option('s', "isSlave", Required = true)]
        public int IsSlave { get; set; }

        [Option('n', "no", Required = true)]
        public int NoOfElements { get; set; }

        [Option('a', "shards", Required = false)]
        public int NoOfShards { get; set; }

        [Option('b', "prefixes", Required = false)]
        public int ReturnPrefixes { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
