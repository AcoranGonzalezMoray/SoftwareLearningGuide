using Dapper;
using MediatR;
using SoftwareLearningGuide.Core.Business.Errors;
using SoftwareLearningGuide.Core.Business.Exceptions;
using System.Data;

namespace SoftwareLearningGuide.Application.Query.GetProduct;

/// <summary>
/// Handler de MediatR para GetProductQuery.
/// Usa Dapper para lectura directa contra la base de datos.
/// </summary>
public sealed class GetProductQueryHandler : IRequestHandler<GetProductQuery, Result<GetProductQueryResponse>> {
    private readonly IDbConnection _connection;

    public GetProductQueryHandler(IDbConnection connection) {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<Result<GetProductQueryResponse>> Handle(GetProductQuery request, CancellationToken cancellationToken) {
        const string sql = @"
            SELECT
                p.Id, p.Name, p.Description,
                p.Price, p.PriceCurrency AS Currency,
                p.StockQuantity,
                p.CreatedAt, p.UpdatedAt
            FROM Products p
            WHERE p.Id = @ProductId;";

        var parameters = new DynamicParameters();
        parameters.Add("ProductId", request.ProductId.ToString(), DbType.String);

        var command = new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken);

        var product = await _connection.QueryFirstOrDefaultAsync<ProductDto>(command);

        if (product is null)
            return Result<GetProductQueryResponse>.Failure(
                DomainErrors.Product.NotFound(request.ProductId));

        return Result<GetProductQueryResponse>.Success(
            new GetProductQueryResponse { Product = product });
    }
}
