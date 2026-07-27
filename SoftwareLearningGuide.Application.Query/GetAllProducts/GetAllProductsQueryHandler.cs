using Dapper;
using MediatR;
using SoftwareLearningGuide.Application.Query.GetProduct;
using SoftwareLearningGuide.Core.Business.Exceptions;
using System.Data;

namespace SoftwareLearningGuide.Application.Query.GetAllProducts;

/// <summary>
/// Handler de MediatR para GetAllProductsQuery.
/// Usa Dapper para lectura directa contra la base de datos.
/// </summary>
public sealed class GetAllProductsQueryHandler : IRequestHandler<GetAllProductsQuery, Result<GetAllProductsQueryResponse>> {
    private readonly IDbConnection _connection;

    public GetAllProductsQueryHandler(IDbConnection connection) {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<Result<GetAllProductsQueryResponse>> Handle(GetAllProductsQuery request, CancellationToken cancellationToken) {
        const string sql = @"
            SELECT
                p.Id, p.Name, p.Description,
                p.Price, p.PriceCurrency AS Currency,
                p.StockQuantity,
                p.CreatedAt, p.UpdatedAt
            FROM Products p
            ORDER BY p.CreatedAt DESC;";

        const string countSql = "SELECT COUNT(*) FROM Products;";

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var products = (await _connection.QueryAsync<ProductDto>(command)).ToList();

        var countCommand = new CommandDefinition(countSql, cancellationToken: cancellationToken);
        var totalCount = await _connection.ExecuteScalarAsync<int>(countCommand);

        return Result<GetAllProductsQueryResponse>.Success(new GetAllProductsQueryResponse {
            Products = products,
            TotalCount = totalCount
        });
    }
}
