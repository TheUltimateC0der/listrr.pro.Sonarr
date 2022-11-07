namespace listrr.pro.Sonarr.Contracts.Models.listrr
{
    public class ListrrListImportSettings
    {
        public string Id { get; set; }

        public int QualityProfileId { get; set; }

        public int LanguageProfileId { get; set; }

        public int RootFolderId { get; set; }

        public bool Monitored { get; set; }

        public bool SeasonFolder { get; set; }

        public bool SearchForMissingEpisodes { get; set; }

        public bool SearchForCutoffUnmetEpisodes { get; set; }
    }
}