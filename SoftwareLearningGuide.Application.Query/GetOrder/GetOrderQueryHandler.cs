using Dapper;
using MediatR;
using SoftwareLearningGuide.Application.Query.GetOrderQuery;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;
using System.Data;

namespace SoftwareLearningGuide.Application.Query.GetOrder;

/// <summary>
/// Handler de MediatR para GetOrderQuery.
/// Usa Dapper para lectura directa contra la base de datos.
/// </summary>
public sealed class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, Result<GetOrderQueryResponse>> {
    private readonly IDbConnection _connection;

    public GetOrderQueryHandler(IDbConnection connection) {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<Result<GetOrderQueryResponse>> Handle(GetOrderQuery request, CancellationToken cancellationToken) {
        const string orderSql = @"
            SELECT
                o.Id, o.CustomerId, o.Status,
                o.ShippingStreet, o.ShippingCity, o.ShippingState,
                o.ShippingPostalCode, o.ShippingCountry,
                o.CreatedAt, o.ConfirmedAt, o.ShippedAt, o.DeliveredAt, o.CancelledAt,
                c.FirstName + ' ' + c.LastName AS CustomerName,
                c.Email AS CustomerEmail
            FROM Orders o
            INNER JOIN Customers c ON c.Id = o.CustomerId
            WHERE o.Id = @OrderId;";

        const string linesSql = @"
            SELECT
                ol.Id, ol.ProductId, ol.ProductName,
                ol.UnitPrice, ol.Currency, ol.Quantity,
                (ol.UnitPrice * ol.Quantity) AS Subtotal
            FROM OrderLines ol
            WHERE ol.OrderId = @OrderId;";

        var orderParameters = new DynamicParameters();
        orderParameters.Add("OrderId", request.OrderId.ToString(), DbType.String);

        var commandOrder = new CommandDefinition(
            orderSql,
            orderParameters,
            cancellationToken: cancellationToken);

        var order = await _connection.QueryFirstOrDefaultAsync<OrderDto>(commandOrder);

        if (order is null)
            return Result<GetOrderQueryResponse>.Failure(DomainErrors.Order.NotFound(request.OrderId));

        var lineParameters = new DynamicParameters();
        lineParameters.Add("OrderId", request.OrderId.ToString(), DbType.String);

        var commandLines = new CommandDefinition(
            linesSql,
            lineParameters,
            cancellationToken: cancellationToken);

        var lines = (await _connection.QueryAsync<OrderLineDto>(commandLines)).ToList();

        var result = order with {
            Lines = lines,
            TotalAmount = lines.Sum(l => l.Subtotal),
            TotalItems = lines.Sum(l => l.Quantity),
            LineCount = lines.Count,
            Currency = lines.FirstOrDefault()?.Currency ?? "USD"
        };

        return Result<GetOrderQueryResponse>.Success(new GetOrderQueryResponse { Order = result });
    }
}
