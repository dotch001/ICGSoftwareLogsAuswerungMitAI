namespace ICGSoftware.Library.LogsAuswerten
{
    public class ApplicationSettingsClass
    {
        public required string ApiKey { get; set; }
        public required string Question { get; set; }
        public required string startTerm { get; set; }
        public required string[] inputFolderPaths { get; set; }
        public string outputFolderPath { get; set; }
        public bool inform { get; set; }
        public bool AskAI { get; set; }
        public required string[] models { get; set; }
        public int chosenModel { get; set; }
        public int maxSizeInKB { get; set; }
    }

}