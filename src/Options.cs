using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotifMe {
    public class Options {
        [Option('p', "prompt", Required = false, HelpText = "Set title of the toast notification")]
        public string Prompt { get; set; }

        [Option('m', "message", Required = true, HelpText = "Set message of the toast notification")]
        public string Message { get; set; }

        [Option('t', "type", Required = false, HelpText = "Set icon of the toast notification")]
        public string Type { get; set; }

        [Option('e', "expiration", Required = false, HelpText = "Number of seconds before the OS mark this notification as expired. (Default: 86400 seconds ; 1 day")]
        public double Expiration { get; set; }

        [Option('d', "duration", Required = false, HelpText = "Make the toast appears longer")]
        public bool Duration { get; set; }

        [Option('s', "Sticky", Required = false, HelpText = "Make the toast appears longer")]
        public bool Sticky { get; set; }
    }
}
