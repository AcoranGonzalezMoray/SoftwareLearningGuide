using Dapper;
using MediatR;
using SoftwareLearningGuide.Core.Business.Exceptions;
using System.Data;

namespace SoftwareLearningGuide.Application.Query.GetAllCustomer;

/// <summary>
/// Handler de MediatR para GetAllCustomersQuery.
/// Usa Dapper para lectura directa contra la base de datos.
/// </summary>
public sealed class GetAllCustomersQueryHandler : IRequestHandler<GetAllCustomersQuery, Result<GetAllCustomersQueryResponse>> {
    private readonly IDbConnection _connection;

    public GetAllCustomersQueryHandler(IDbConnection connection) {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<Result<GetAllCustomersQueryResponse>> Handle(GetAllCustomersQuery request, CancellationToken cancellationToken) {
        const string sql = @"
            SELECT
                c.Id, c.FirstName, c.LastName,
                c.Email, c.CreatedAt,
                (SELECT COUNT(*) FROM Orders o WHERE o.CustomerId = c.Id) AS OrderCount
            FROM Customers c
            ORDER BY c.CreatedAt DESC;";

        const string countSql = "SELECT COUNT(*) FROM Customers;";

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var customers = (await _connection.QueryAsync<CustomerSummaryDto>(command)).ToList();

        var countCommand = new CommandDefinition(countSql, cancellationToken: cancellationToken);
        var totalCount = await _connection.ExecuteScalarAsync<int>(countCommand);

        return Result<GetAllCustomersQueryResponse>.Success(new GetAllCustomersQueryResponse {
            Customers = customers,
            TotalCount = totalCount
        });
    }
}
