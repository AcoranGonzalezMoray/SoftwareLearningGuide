namespace SoftwareLearningGuide.Consumer.Options;

public sealed class MessageBrokerOptions {
    public static string SectionName => "MessageBroker";
    public string Host { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}
