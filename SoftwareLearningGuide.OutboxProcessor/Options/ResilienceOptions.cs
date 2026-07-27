namespace SoftwareLearningGuide.OutboxProcessor.Options;

public sealed class ResilienceOptions {
    public static string SectionName => "ResilienceConfiguration";
    public MessageBrokerResilienceOptions MessageBrokerApi { get; set; } = new();
}

public sealed class MessageBrokerResilienceOptions {
    public RetryPolicyOptions Retry { get; set; } = new();
    public CircuitBreakerPolicyOptions CircuitBreaker { get; set; } = new();
    public TimeoutPolicyOptions TimeOut { get; set; } = new();
}

public sealed class RetryPolicyOptions {
    public int MaxRetryCount { get; set; } = 3;
    public int InitialRetryIntervalSeconds { get; set; } = 1;
    public int MaxRetryIntervalSeconds { get; set; } = 30;
    public int IntervalDeltaSeconds { get; set; } = 5;
}

public sealed class CircuitBreakerPolicyOptions {
    public int TripThreshold { get; set; } = 15;
    public int ActiveThreshold { get; set; } = 10;
    public int TrackingPeriodMinutes { get; set; } = 1;
    public int ResetIntervalMinutes { get; set; } = 5;
}

public sealed class TimeoutPolicyOptions {
    public int TimeoutSeconds { get; set; } = 30;
}
