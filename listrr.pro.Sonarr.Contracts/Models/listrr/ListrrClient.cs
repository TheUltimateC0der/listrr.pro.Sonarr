using RestSharp;

namespace listrr.pro.Sonarr.Contracts.Models.listrr
{
    public class ListrrClient
    {
        private readonly RestClient _restClient;

        public ListrrClient(string url, string apiKey)
        {
            _restClient = new RestClient(url)
                .AddDefaultHeader("X-Api-Key", apiKey)
                .AddDefaultHeader(KnownHeaders.Accept, "application/json");
        }


        public async Task<List<ListrrListContent>> GetList(string id) => await _restClient.GetJsonAsync<List<ListrrListContent>>($"api/Import/Sonarr/{id}");

        public async Task<List<ListrrList>> GetLists() => await _restClient.GetJsonAsync<List<ListrrList>>("api/Import/ShowLists");
    }
}