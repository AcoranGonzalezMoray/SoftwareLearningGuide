using Dapper;
using MediatR;
using SoftwareLearningGuide.Core.Business.Exceptions;
using System.Data;

namespace SoftwareLearningGuide.Application.Query.GetAllOrders;

/// <summary>
/// Handler de MediatR para GetAllOrdersQuery.
/// Usa Dapper para lectura directa contra la base de datos.
/// </summary>
public sealed class GetAllOrdersQueryHandler : IRequestHandler<GetAllOrdersQuery, Result<GetAllOrdersQueryResponse>> {
    private readonly IDbConnection _connection;

    public GetAllOrdersQueryHandler(IDbConnection connection) {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<Result<GetAllOrdersQueryResponse>> Handle(GetAllOrdersQuery request, CancellationToken cancellationToken) {
        const string sql = @"
            SELECT
                o.Id, o.CustomerId,
                c.FirstName + ' ' + c.LastName AS CustomerName,
                o.Status,
                o.CreatedAt
            FROM Orders o
            INNER JOIN Customers c ON c.Id = o.CustomerId
            ORDER BY o.CreatedAt DESC;";

        const string countSql = "SELECT COUNT(*) FROM Orders;";

        const string linesCountSql = @"
            SELECT ol.OrderId, COUNT(*) AS LineCount
            FROM OrderLines ol
            GROUP BY ol.OrderId;";

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var orders = (await _connection.QueryAsync<OrderSummaryDto>(command)).ToList();

        var countCommand = new CommandDefinition(countSql, cancellationToken: cancellationToken);
        var totalCount = await _connection.ExecuteScalarAsync<int>(countCommand);

        var linesCommand = new CommandDefinition(linesCountSql, cancellationToken: cancellationToken);
        var linesCounts = (await _connection.QueryAsync<(Guid OrderId, int LineCount)>(linesCommand))
            .ToDictionary(x => x.OrderId, x => x.LineCount);

        var ordersWithLines = orders.Select(o => o with {
            LineCount = linesCounts.TryGetValue(o.Id, out var count) ? count : 0
        }).ToList();

        return Result<GetAllOrdersQueryResponse>.Success(new GetAllOrdersQueryResponse {
            Orders = ordersWithLines,
            TotalCount = totalCount
        });
    }
}
