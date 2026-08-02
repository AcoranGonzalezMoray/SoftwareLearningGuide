using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using SoftwareLearningGuide.Api.Extensions;
using SoftwareLearningGuide.Api.FeatureToggles;
using SoftwareLearningGuide.Application.Command.CreateProduct;
using SoftwareLearningGuide.Application.Query.GetAllProducts;
using SoftwareLearningGuide.Application.Query.GetProduct;

namespace SoftwareLearningGuide.Api.Controllers.ProductControllerExample;

/// <summary>
/// Controller de Products que demuestra CQRS con MediatR.
/// El controller NO conoce los handlers: solo despacha queries/commands
/// y MediatR resuelve automaticamente el handler correspondiente.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = CognitoPolicies.RequireNormalRole)]
[FeatureGate(FeatureToggleNames.FT_ENABLE_PRODUCT_CONTROLLER)]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductController : ControllerBase {
    private readonly IMediator _mediator;
    private readonly ILogger<ProductController> _logger;

    public ProductController(IMediator mediator, ILogger<ProductController> logger) {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET api/v1/product
    /// Obtiene todos los productos usando GetAllProductsQuery (Dapper, lectura).
    /// </summary>
    [HttpGet]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_PRODUCT_LIST)]
    [ProducesResponseType(typeof(GetAllProductsQueryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", HttpContext.TraceIdentifier }
        })) {
            _logger.LogInformation("Iniciando obtencion de todos los productos");

            var query = new GetAllProductsQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess) {
                _logger.LogWarning("Error al obtener productos: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Se obtuvieron {Count} productos", result.Value!.TotalCount);
            return Ok(result.Value);
        }
    }

    /// <summary>
    /// GET api/v1/product/{id}
    /// Obtiene un producto por su ID usando GetProductQuery (Dapper, lectura).
    /// </summary>
    [HttpGet("{id:guid}")]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_PRODUCT_RETRIEVAL)]
    [ProducesResponseType(typeof(GetProductQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", HttpContext.TraceIdentifier },
            { "RequestMethod", HttpContext.Request.Method },
            { "RequestPath", HttpContext.Request.Path },
            { "RemoteIpAddress", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown" },
            { "ProductId", id }
        })) {
            _logger.LogInformation("Iniciando obtencion de producto {ProductId}", id);

            var query = new GetProductQuery(id);
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess) {
                _logger.LogWarning("Producto {ProductId} no encontrado: {Error}", id, result.Error);
                return NotFound(new { error = result.Error });
            }

            _logger.LogInformation("Producto {ProductId} obtenido exitosamente", id);
            return Ok(result.Value);
        }
    }

    /// <summary>
    /// POST api/v1/product
    /// Crea un nuevo producto usando CreateProductCommand (EF Core, escritura).
    /// </summary>
    [HttpPost]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_PRODUCT_CREATION)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken cancellationToken) {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", HttpContext.TraceIdentifier },
            { "RequestMethod", HttpContext.Request.Method },
            { "RequestPath", HttpContext.Request.Path },
            { "RemoteIpAddress", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown" },
            { "ProductName", command.Name },
            { "Price", command.Price },
            { "Currency", command.Currency }
        })) {
            _logger.LogInformation(
                "Iniciando creacion de producto {ProductName} con precio {Price} {Currency}",
                command.Name, command.Price, command.Currency);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess) {
                _logger.LogWarning(
                    "Error al crear producto {ProductName}: {Error}",
                    command.Name, result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation(
                "Producto {ProductId} creado exitosamente",
                result.Value);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Value },
                new { productId = result.Value });
        }
    }
}
