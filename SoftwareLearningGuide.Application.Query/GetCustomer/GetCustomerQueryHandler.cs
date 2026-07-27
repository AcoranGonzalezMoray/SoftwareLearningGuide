using Dapper;
using MediatR;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;
using System.Data;

namespace SoftwareLearningGuide.Application.Query.GetCustomer;

/// <summary>
/// Handler de MediatR para GetCustomerQuery.
/// Usa Dapper para lectura directa contra la base de datos.
/// </summary>
public sealed class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, Result<GetCustomerQueryResponse>> {
    private readonly IDbConnection _connection;

    public GetCustomerQueryHandler(IDbConnection connection) {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<Result<GetCustomerQueryResponse>> Handle(GetCustomerQuery request, CancellationToken cancellationToken) {
        const string sql = @"
            SELECT
                c.Id, c.FirstName, c.LastName,
                c.Email,
                c.DefaultStreet, c.DefaultCity, c.DefaultState,
                c.DefaultPostalCode, c.DefaultCountry,
                c.CreatedAt, c.UpdatedAt,
                (SELECT COUNT(*) FROM Orders o WHERE o.CustomerId = c.Id) AS OrderCount
            FROM Customers c
            WHERE c.Id = @CustomerId;";

        var parameters = new DynamicParameters();
        parameters.Add("CustomerId", request.CustomerId.ToString(), DbType.String);

        var command = new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken);

        var customer = await _connection.QueryFirstOrDefaultAsync<CustomerDto>(command);

        if (customer is null)
            return Result<GetCustomerQueryResponse>.Failure(DomainErrors.Customer.NotFound(request.CustomerId));

        return Result<GetCustomerQueryResponse>.Success(new GetCustomerQueryResponse { Customer = customer });
    }
}
