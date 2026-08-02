using MassTransit;

namespace SoftwareLearningGuide.Consumer.Buses;

/// <summary>
/// Interfaz marcadora que identifica el segundo bus de MassTransit (transporte AWS SQS/SNS),
/// permitiendo convivir con el bus predeterminado de RabbitMQ.
/// </summary>
public interface IAwsMessageBus : IBus;
