using CommandLine;

using listrr.pro.Sonarr.Contracts.Models;
using listrr.pro.Sonarr.Contracts.Models.listrr;
using listrr.pro.Sonarr.Contracts.Models.Starr;
using listrr.pro.Sonarr.Contracts.Models.Starr.Sonarr;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Spectre.Console;

using System.Text;

namespace listrr.pro.Sonarr
{
    internal class Program
    {
        private static ListrrAutoImportSettings autoImportSettings = new();
        private static List<ListrrListImportSettings> listrrListImportSettings = new List<ListrrListImportSettings>();
        private static SonarrInstance sonarrInstance = new();

        private static bool loop = true;

        static async Task Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var configuration = builder.Build();

            ConfigurationBinder.Bind(configuration.GetSection("SonarrInstance"), sonarrInstance);
            ConfigurationBinder.Bind(configuration.GetSection("listrr").GetSection("AutoImport"), autoImportSettings);
            ConfigurationBinder.Bind(configuration.GetSection("listrr").GetSection("Lists"), listrrListImportSettings);

            var sonarrValidated = await ValidateSonarrInstance();
            if (!sonarrValidated)
                return;

            await Parser.Default.ParseArguments<Options>(args).WithParsedAsync(RunOptions);
        }

        static async Task RunOptions(Options opts)
        {
            opts.Verbose = false;
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            while (loop)
            {
                var overallStats = new Stats();

                Log(LogLevel.None, $"Connecting to Sonarr: {sonarrInstance.Url} with API Key: '{sonarrInstance.ApiKey}'");

                var sonarrClient = new SonarrClient(sonarrInstance.Url, sonarrInstance.ApiKey);
                var listrrClient = new ListrrClient("https://listrr.pro", autoImportSettings.ApiKey);

                var qualityProfiles = await sonarrClient.GetQualityProfiles();
                foreach (var qualityProfile in qualityProfiles)
                {
                    Log(LogLevel.Information, $"Found QualityProfile {qualityProfile.Id}:{qualityProfile.Name}");
                }

                var rootFolders = await sonarrClient.GetRootFolders();
                foreach (var rootFolder in rootFolders)
                {
                    Log(LogLevel.Information, $"Found RootFolder {rootFolder.Id}:{rootFolder.Path}");
                }

                var languageProfiles = await sonarrClient.GetLanguageProfiles();
                foreach (var languageProfile in languageProfiles)
                {
                    Log(LogLevel.Information, $"Found LanguageProfile {languageProfile.Id}:{languageProfile.Name}");
                }

                var tags = await sonarrClient.GetTags();
                foreach (var tag in tags)
                {
                    Log(LogLevel.Information, $"Found Tag {tag.Id}:{tag.Label}");
                }

                Log(LogLevel.Information, $"Please use the provided information above to set your settings according to the documentation!");

                if (!autoImportSettings.ImportLists && !listrrListImportSettings.Any())
                {
                    Log(LogLevel.Critical, $"There is nothing I can import. There are no manual lists, and AutoImport is disabled.");
                    return;
                }

                if (autoImportSettings.ImportLists && (autoImportSettings.LanguageProfileId == 0 || autoImportSettings.QualityProfileId == 0 || autoImportSettings.RootFolderId == 0))
                    Log(LogLevel.Information, $"Skipping AutoImport. It is either set to false, or some of the Ids you need to set are set to 0 or not set at all.");

                if (!listrrListImportSettings.Any())
                    Log(LogLevel.Information, $"Skipping manual mode. There are no lists present.");





                Log(LogLevel.None, $"Getting existing series from Sonarr...");
                var existingSeries = await sonarrClient.GetSeries();
                Log(LogLevel.None, $"Got existing series from Sonarr!");

                var immediateExit = false;
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Adding lists to Sonarr instance!", async ctx =>
                    {
                        if (autoImportSettings.ImportLists)
                        {
                            ctx.Status($"--- AUTO IMPORT MODE ---");
                            await Task.Delay(5000);

                            ctx.Status($"Getting lists from listrr.pro account...");

                            var listIds = await listrrClient.GetLists();
                            Log(LogLevel.None, $"Got all lists from listrr.pro account!");

                            foreach (var listId in listIds)
                            {
                                if (immediateExit)
                                    break;

                                var listrrListContent = await listrrClient.GetList(listId.Id);

                                ctx.Status($"Working on listrr list: {listId.Name}");

                                try
                                {
                                    var stats = await ProcessList(opts, ctx, sonarrClient, listrrListContent, existingSeries, rootFolders.First(x => x.Id == autoImportSettings.RootFolderId).Path, listId.Name);

                                    ShowStats(stats, listId.Name);

                                    overallStats.Failed += stats.Failed;
                                    overallStats.Added += stats.Added;
                                    overallStats.Existing += stats.Existing;
                                    overallStats.Shows += stats.Shows;
                                }
                                catch (TimeoutException e)
                                {
                                    immediateExit = true;
                                    Log(LogLevel.Debug, $"We got a timeout while adding a show to Sonarr. Sonarr seems to be overloaded with requests. We skip for now, and try later.");
                                }
                            }
                        }

                        if (immediateExit)
                        {
                            Log(LogLevel.Warning, $"Skipping 'LISTS MODE' because of previous timeout");

                            return;
                        }

                        ctx.Status($"--- LISTS MODE ---");
                        await Task.Delay(5000);

                        if (!listrrListImportSettings.Any())
                        {
                            ctx.Status($"No lists to import.");
                            await Task.Delay(5000);
                        }

                        foreach (var listImportSetting in listrrListImportSettings)
                        {
                            if (immediateExit)
                                break;

                            ctx.Status($"Getting list content for: {listImportSetting.Id}");

                            var listrrListContent = await listrrClient.GetList(listImportSetting.Id);

                            try
                            {
                                var stats = await ProcessList(opts, ctx, sonarrClient, listrrListContent, existingSeries, rootFolders.First(x => x.Id == listImportSetting.RootFolderId).Path, listImportSetting.Id, listImportSetting);

                                ShowStats(stats, listImportSetting.Id);

                                overallStats.Failed += stats.Failed;
                                overallStats.Added += stats.Added;
                                overallStats.Existing += stats.Existing;
                                overallStats.Shows += stats.Shows;
                            }
                            catch (TimeoutException e)
                            {
                                immediateExit = true;
                                Log(LogLevel.Debug, $"We got a timeout while adding a show to Sonarr. Sonarr seems to be overloaded with requests. We skip for now, and try later.");
                            }
                        }
                    });


                if (loop)
                {
                    ShowStats(overallStats, "Overall");

                    await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .StartAsync("Sleeping 3h until proceeding!", async ctx =>
                        {
                            await Task.Delay(10800000);
                        });
                }
            }
        }

        private static async Task<Stats> ProcessList(Options opts, StatusContext ctx, SonarrClient sonarrClient, IList<ListrrListContent> listrrListContent, IList<GetSeriesRequest> existingSeries, string rootFolderPath, string listNameOrId, ListrrListImportSettings importSettings = null)
        {
            var stats = new Stats();

            foreach (var listContent in listrrListContent)
            {
                stats.Shows++;

                if (existingSeries.Any(x => x.TvdbId == listContent.TvdbId))
                {
                    if (opts.Verbose)
                        Log(LogLevel.Debug, $"Show with TVDB ID '{listContent.TvdbId}' already exists. Skipping ...");

                    stats.Existing++;

                    continue;
                }

                if (opts.Verbose)
                    Log(LogLevel.Debug, $"Asking Sonarr for TVDB ID '{listContent.TvdbId}' ...");

                var results = await sonarrClient.SeriesLookup($"tvdb:{listContent.TvdbId}");
                if (results.Count >= 1)
                {
                    if (opts.Verbose)
                        Log(LogLevel.Debug, $"Adding TVDB ID '{listContent.TvdbId}' ...");

                    var cleanName = results.First().Title.Replace("[", "").Replace("]", "");

                    Log(LogLevel.Information, $"Adding from '{listNameOrId}' to Sonarr - '{listContent.TvdbId}' - '{cleanName}'");

                    try
                    {
                        var addTitle = $"{cleanName} ({results.First().Year})";

                        if (importSettings == null)
                        {
                            await sonarrClient.AddSeries(new AddSeriesRequest()
                            {
                                LanguageProfileId = autoImportSettings.LanguageProfileId,
                                QualityProfileId = autoImportSettings.QualityProfileId,
                                RootFolderPath = rootFolderPath,
                                Title = addTitle,
                                TvdbId = listContent.TvdbId,
                                Monitored = autoImportSettings.Monitored,
                                SeasonFolder = autoImportSettings.SeasonFolder,
                                Tags = autoImportSettings.Tags,
                                AddOptions = new AddSeriesRequestOptions()
                                {
                                    SearchForCutoffUnmetEpisodes = autoImportSettings.SearchForCutoffUnmetEpisodes,
                                    SearchForMissingEpisodes = autoImportSettings.SearchForMissingEpisodes
                                }
                            });
                        }
                        else
                        {
                            await sonarrClient.AddSeries(new AddSeriesRequest()
                            {
                                LanguageProfileId = importSettings.LanguageProfileId,
                                QualityProfileId = importSettings.QualityProfileId,
                                RootFolderPath = rootFolderPath,
                                Title = addTitle,
                                TvdbId = listContent.TvdbId,
                                Monitored = importSettings.Monitored,
                                SeasonFolder = importSettings.SeasonFolder,
                                Tags = importSettings.Tags,
                                AddOptions = new AddSeriesRequestOptions()
                                {
                                    SearchForCutoffUnmetEpisodes = importSettings.SearchForCutoffUnmetEpisodes,
                                    SearchForMissingEpisodes = importSettings.SearchForMissingEpisodes
                                }
                            });
                        }


                        stats.Added++;
                    }
                    catch (HttpRequestException e)
                    {
                        stats.Failed++;

                        Log(LogLevel.Error, $"Sonarr responded with ErrorCode: {e.StatusCode}. TVDB ID: '{listContent.TvdbId}' Name: '{cleanName}'");
                    }

                    if (opts.Verbose)
                        Log(LogLevel.Debug, $"Added TVDB ID '{listContent.TvdbId}' to Sonarr ...");
                }
                else
                {
                    stats.Failed++;

                    Log(LogLevel.Warning, $"Sonarr returned nothing for TVDB ID '{listContent.TvdbId}'. So we cannot add it.");
                }

                ctx.Status("Waiting 3s before adding the next show...");
                await Task.Delay(3000);
            }

            return stats;
        }

        private static void ShowStats(Stats statistic, string listNameOrId)
        {
            AnsiConsole.Write(
                new Panel(new Text($"Shows: {statistic.Shows}\r\nAdded: {statistic.Added}\r\nExisting: {statistic.Existing}\r\nFailed: {statistic.Failed}"))
                    .RoundedBorder()
                    .Header($"  [green]{listNameOrId} stats[/]  ")
                    .HeaderAlignment(Justify.Center));
        }

        private static async Task<bool> ValidateSonarrInstance()
        {
            if (sonarrInstance == null)
            {
                Log(LogLevel.Error, "Please set the information for your Sonarr instance.");
                return false;
            }
            if (string.IsNullOrEmpty(sonarrInstance.Url))
            {
                Log(LogLevel.Error, "Please set the Url of your Sonarr instance.");
                return false;
            }
            if (string.IsNullOrEmpty(sonarrInstance.ApiKey))
            {
                Log(LogLevel.Error, "Please set the ApiKey of your Sonarr instance.");
                return false;
            }

            var client = new SonarrClient(sonarrInstance.Url, sonarrInstance.ApiKey);
            await client.SeriesLookup($"tvdb:71663");

            return true;
        }

        private static void Log(LogLevel logLevel, string text)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    AnsiConsole.MarkupLine($"[purple4]TRC:[/] [silver]{text}[/]");

                    break;
                case LogLevel.Debug:
                    AnsiConsole.MarkupLine($"[darkblue]DBG:[/] [silver]{text}[/]");

                    break;
                case LogLevel.Information:
                    AnsiConsole.MarkupLine($"[dodgerblue2]INFO:[/] [silver]{text}[/]");

                    break;
                case LogLevel.Warning:
                    AnsiConsole.MarkupLine($"[red]WARN:[/] [yellow]{text}[/]");

                    break;
                case LogLevel.Error:
                    AnsiConsole.MarkupLine($"[red]ERR:[/] [yellow]{text}[/]");

                    break;
                case LogLevel.Critical:
                    AnsiConsole.MarkupLine($"[yellow]CRIT:[/] [red]{text}[/]");

                    break;
                case LogLevel.None:
                    AnsiConsole.MarkupLine($"[gray]LOG:[/] [silver]{text}[/]");

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null);
            }
        }
    }
}