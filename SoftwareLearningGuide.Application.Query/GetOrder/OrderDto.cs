namespace SoftwareLearningGuide.Application.Query.GetOrder;

/// <summary>
/// DTO de lectura para Order. Proyecta los datos necesarios para la capa de presentación.
/// </summary>
public sealed record OrderDto {
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public int TotalItems { get; init; }
    public int LineCount { get; init; }
    public string ShippingStreet { get; init; } = string.Empty;
    public string ShippingCity { get; init; } = string.Empty;
    public string ShippingState { get; init; } = string.Empty;
    public string ShippingPostalCode { get; init; } = string.Empty;
    public string ShippingCountry { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? ShippedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public List<OrderLineDto> Lines { get; init; } = new();
}

/// <summary>
/// DTO de lectura para OrderLine.
/// </summary>
public sealed record OrderLineDto {
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Subtotal { get; init; }
}
