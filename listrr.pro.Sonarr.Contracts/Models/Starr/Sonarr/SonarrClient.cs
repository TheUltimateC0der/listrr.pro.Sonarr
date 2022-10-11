using RestSharp;

using System.Text.Json.Serialization;

namespace listrr.pro.Sonarr.Contracts.Models.Starr.Sonarr
{
    public class SonarrClient
    {
        private readonly RestClient _restClient;

        public SonarrClient(string url, string apiKey)
        {
            _restClient = new RestClient(url)
                .AddDefaultHeader("X-Api-Key", apiKey)
                .AddDefaultHeader(KnownHeaders.Accept, "application/json");
        }

        public async Task<List<QualityProfile>> GetQualityProfiles() => await _restClient.GetJsonAsync<List<QualityProfile>>("api/v3/qualityprofile");

        public async Task<List<RootFolder>> GetRootFolders() => await _restClient.GetJsonAsync<List<RootFolder>>("api/v3/rootfolder");

        public async Task<List<LanguageProfile>> GetLanguageProfiles() => await _restClient.GetJsonAsync<List<LanguageProfile>>("api/v3/languageprofile");

        public async Task<List<SeriesLookupResponse>> SeriesLookup(string term) => await _restClient.GetJsonAsync<List<SeriesLookupResponse>>("api/v3/series/lookup", new { term = term });

        public async Task AddSeries(AddSeriesRequest series) => await _restClient.PostJsonAsync("api/v3/series", series);

        public async Task<List<GetSeriesRequest>> GetSeries() => await _restClient.GetJsonAsync<List<GetSeriesRequest>>("api/v3/series");

    }


    public class GetSeriesRequest
    {
        public string Title { get; set; }

        public int TvdbId { get; set; }
    }

    public class AddSeriesRequest
    {
        public string Title { get; set; }

        public int TvdbId { get; set; }

        public bool Monitored { get; set; }

        public int QualityProfileId { get; set; }

        public int LanguageProfileId { get; set; }

        public string RootFolderPath { get; set; }

        public AddSeriesRequestOptions AddOptions { get; set; }
    }

    public class AddSeriesRequestOptions
    {
        [JsonPropertyName("monitor")]
        public string Monitor => "all";

        [JsonPropertyName("searchForMissingEpisodes")]
        public bool SearchForMissingEpisodes { get; set; }

        [JsonPropertyName("searchForCutoffUnmetEpisodes")]
        public bool SearchForCutoffUnmetEpisodes { get; set; }
    }


    public class SeriesLookupResponse
    {
        public string Title { get; set; }

        public string SortTitle { get; set; }

        public int Year { get; set; }
    }
}