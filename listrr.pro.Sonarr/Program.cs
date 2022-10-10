using CommandLine;

using listrr.pro.Sonarr.Contracts.Models.listrr;
using listrr.pro.Sonarr.Contracts.Models.Starr;

using Microsoft.Extensions.Configuration;

using SonarrSharp;

namespace listrr.pro.Sonarr
{
    internal class Program
    {
        private static ListrrAutoImportSettings listrrAutoImportSettings;
        private static SonarrInstance sonarrInstance;
        private static IList<ListrrList> listrrLists;

        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables("listrr__");

            var configuration = builder.Build();

            ConfigurationBinder.Bind(configuration.GetSection("SonarrInstance"), sonarrInstance);
            ConfigurationBinder.Bind(configuration.GetSection("listrr").GetSection("AutoImport"), listrrAutoImportSettings);
            ConfigurationBinder.Bind(configuration.GetSection("listrr").GetSection("Lists"), listrrLists);


            await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(RunOptions);
        }

        static async Task RunOptions(Options opts)
        {
            while (true)
            {
                var sonarrClient = new SonarrClient(sonarrInstance.Host, sonarrInstance.Port, sonarrInstance.ApiKey);

                var foundSeries = await sonarrClient.SeriesLookup.SearchForSeries(399959);


                await Task.Delay(10800000);
            }
        }

        static void HandleParseError(IEnumerable<Error> errs)
        {
            //handle errors
        }
    }
}