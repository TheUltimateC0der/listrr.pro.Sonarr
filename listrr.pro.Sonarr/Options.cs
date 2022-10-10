using CommandLine;

namespace listrr.pro.Sonarr
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option("ids", Required = false, HelpText = "List all your Quality profiles and Root folders to see their Ids")]
        public bool ListIds { get; set; }
    }
}