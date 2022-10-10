namespace listrr.pro.Sonarr.Contracts.Models.listrr
{
    public class ListrrAutoImportSettings
    {
        public bool ImportLists { get; set; }

        public string ApiKey { get; set; }

        public int QualityProfileId { get; set; }

        public int RootFolderId { get; set; }
    }
}