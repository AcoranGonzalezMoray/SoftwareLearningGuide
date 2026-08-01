namespace SoftwareLearningGuide.Consumer.Options {
    public class AwsConfigurationOptions {
        public bool Enabled { get; set; }

        public AwsCredentialsOptions Credentials { get; set; } = new();

        public AwsSSMConfigurationOptions SSM { get; set; } = new();

        public class AwsCredentialsOptions {
            public string AccessKey { get; set; } = string.Empty;

            public string AccessSecret { get; set; } = string.Empty;
        }

        public class AwsSSMConfigurationOptions {
            public static string SectionName => "SSM";

            public bool Enabled { get; set; }

            public string Path { get; set; } = string.Empty;

            public string Region { get; set; } = "us-east-1";

            public string ServiceUrl { get; set; } = string.Empty;

            public int ReloadIntervalSeconds { get; set; } = 300;
        }
    }
}
