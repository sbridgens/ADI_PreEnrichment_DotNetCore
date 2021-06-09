namespace Application.Configuration
{
    public class EnrichmentSettings
    {
        public string InputDirectory { get; set; }
        public int PollIntervalInSeconds { get; set; }
        public string AllowSDContentIngest { get; set; }
        public string UnrequiredSDContentDirectory { get; set; }
        public string TempWorkingDirectory { get; set; }
        public int RequiredFreeSpaceInGigabytesInTempWorkingDirectory { get; set; }
        public string FailedDirectory { get; set; }
        public string IngestDirectory { get; set; }
        public string UpdatesFailedDirectory { get; set; }
        public string MoveNonMappedDirectory { get; set; }
        public bool ProcessMappingFailures { get; set; }
        public string RepollNonMappedIntervalHours { get; set; }
        public int FailedToMap_Max_Retry_Days { get; set; }
        public string TVOD_Delivery_Directory { get; set; }
        public string OnApi { get; set; }
        public string ApiKey { get; set; }
        public string MediaCloud { get; set; }
        public string Prefix_Show_ID_Value { get; set; }
        public string Prefix_Series_ID_Value { get; set; }
        public AllowAdultContentIngest AllowAdultContentIngest { get; set; }
        public ProcessUHDContent ProcessUHDContent { get; set; }
        public Block_Platform Block_Platform { get; set; }
    }

    public class AllowAdultContentIngest
    {
        public string DeliveryDirectory { get; set; }
        public bool AllowAdultEnrichment { get; set; }
    }

    public class ProcessUHDContent
    {
        public string UHD_Delivery_Directory { get; set; }
        public bool AllowUHDContentIngest { get; set; }
    }

    public class Block_Platform
    {
        public string Providers { get; set; }
        public string BlockPlatformValue { get; set; }
    }
}