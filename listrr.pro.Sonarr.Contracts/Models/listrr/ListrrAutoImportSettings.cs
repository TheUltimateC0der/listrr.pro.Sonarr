namespace listrr.pro.Sonarr.Contracts.Models.listrr
{
    public class ListrrAutoImportSettings
    {
        public bool ImportLists { get; set; }

        public string ApiKey { get; set; }

        public int QualityProfileId { get; set; }

        public int LanguageProfileId { get; set; }

        public int RootFolderId { get; set; }

        public bool Monitored { get; set; }

        public bool SearchForMissingEpisodes { get; set; }

        public bool SearchForCutoffUnmetEpisodes { get; set; }
    }
}