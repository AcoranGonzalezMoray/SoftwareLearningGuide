namespace SoftwareLearningGuide.Api.Options {
    public class DatabaseOptions {
        public static string SectionName => "ConnectionStrings";
        public string SoftwareLearningGuide { get; set; } = string.Empty;
    }
}
