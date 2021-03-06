﻿using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bluepath.Autocomplete
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

        [Option('f', "folder", Required = true)]
        public string InputFolder { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
