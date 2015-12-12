namespace StyleCopAnalyzers.Status.Website.Models
{
    public class StatusPageOptions
    {
        public string DefaultBranch { get; set; } = "master";
        public string DataDirectory { get; set; } = "data";

        public string DataUri { get; set; } = "http://stylecopdata.root.pdelvo.com/data/";

        public DataProvider DataProvider { get; set; } = DataProvider.File;
    }

    public enum DataProvider
    {
        File,
        Web
    }
}
