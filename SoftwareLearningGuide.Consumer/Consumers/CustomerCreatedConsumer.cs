using MassTransit;
using SoftwareLearningGuide.Contracts.IntegrationEvents;

namespace SoftwareLearningGuide.Consumer.Consumers;

/// <summary>
/// Consumer de MassTransit que procesa el evento CustomerCreatedEvent
/// desde el transporte AWS (SQS/SNS). Es un "consumer extra" que recibe
/// el evento por SNS mientras los demás consumers lo hacen por RabbitMQ.
/// </summary>
public sealed class CustomerCreatedConsumer : IConsumer<CustomerCreatedEvent> {
    private readonly ILogger<CustomerCreatedConsumer> _logger;

    public CustomerCreatedConsumer(ILogger<CustomerCreatedConsumer> logger) {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<CustomerCreatedEvent> context) {
        var message = context.Message;

        _logger.LogWarning(
            "[CustomerCreatedConsumer] (SNS/SQS) Customer created: CustomerId={CustomerId}, Name={Name}, Email={Email}",
            message.CustomerId,
            message.Name,
            message.Email);

        await Task.CompletedTask;
    }
}
