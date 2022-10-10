namespace listrr.pro.Sonarr.Contracts.Models.Starr
{
    public class SonarrInstance
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public string ApiKey { get; set; }
    }
}