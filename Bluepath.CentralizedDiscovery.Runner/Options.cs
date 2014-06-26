using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.CentralizedDiscovery.Runner
{
    class Options
    {
        [Option('i', "ip", Required = true)]
        public string Ip { get; set; }

        [Option('p', "port", Required = false)]
        public int? Port { get; set; }

        [Option('r', "redis", Required = true)]
        public string RedisConf { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
