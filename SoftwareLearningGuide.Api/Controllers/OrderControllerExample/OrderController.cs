using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;
using SoftwareLearningGuide.Api.FeatureToggles;
using SoftwareLearningGuide.Api.Metrics;
using SoftwareLearningGuide.Application.Command.CreateOrder;
using SoftwareLearningGuide.Application.Query.GetAllOrders;
using SoftwareLearningGuide.Application.Query.GetOrder;
using SoftwareLearningGuide.Application.Query.GetOrderQuery;

namespace SoftwareLearningGuide.Api.Controllers.OrderControllerExample;

/// <summary>
/// Controller de Orders que demuestra CQRS con MediatR.
/// El controller NO conoce los handlers: solo despacha queries/commands
/// y MediatR resuelve automáticamente el handler correspondiente.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_CONTROLLER)]
[Route("api/v{version:apiVersion}/[controller]")]
public class OrderController : ControllerBase {
    private readonly IMediator _mediator;
    private readonly ILogger<OrderController> _logger;
    private readonly OrderMetrics _metrics;
    private readonly IFeatureManagerSnapshot _featureManager;

    public OrderController(IMediator mediator, ILogger<OrderController> logger, OrderMetrics metrics, IFeatureManagerSnapshot featureManager) {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _featureManager = featureManager ?? throw new ArgumentNullException(nameof(featureManager));
    }

    /// <summary>
    /// GET api/v1/order
    /// Obtiene todas las ordenes usando GetAllOrdersQuery (Dapper, lectura).
    /// </summary>
    [HttpGet]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_LIST)]
    [ProducesResponseType(typeof(GetAllOrdersQueryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", HttpContext.TraceIdentifier }
        })) {
            _logger.LogInformation("Iniciando obtencion de todas las ordenes");

            var query = new GetAllOrdersQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess) {
                _logger.LogWarning("Error al obtener ordenes: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Se obtuvieron {Count} ordenes", result.Value!.TotalCount);
            return Ok(result.Value);
        }
    }

    /// <summary>
    /// GET api/v1/order/{id}
    /// Obtiene una orden por su ID usando GetOrderQuery (Dapper, lectura).
    /// </summary>
    [HttpGet("{id:guid}")]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_ORDER_RETRIEVAL)]
    [ProducesResponseType(typeof(GetOrderQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "OrderId", id }
        })) {
            _logger.LogInformation("Iniciando obtención de orden {OrderId}", id);

            var query = new GetOrderQuery(id);
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess) {
                _logger.LogWarning("Orden {OrderId} no encontrada: {Error}", id, result.Error);
                return NotFound(new { error = result.Error });
            }

            _logger.LogInformation("Orden {OrderId} obtenida exitosamente", id);
            return Ok(result.Value);
        }
    }

    /// <summary>
    /// POST api/v1/order
    /// Crea una nueva orden usando CreateOrderCommand (EF Core, escritura).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken cancellationToken) {

        if (await _featureManager.IsEnabledAsync(FeatureToggleNames.FT_ENABLE_ORDER_CREATION) is false) {
            _logger.LogWarning("Intento de crear orden mientras el módulo ORDERS_MODULE está deshabilitado.");
            return NotFound();
        }

        HttpContext.Items["CustomerId"] = command.CustomerId;
        HttpContext.Items["LineCount"] = command.Lines.Count;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CustomerId", command.CustomerId },
            { "LineCount", command.Lines.Count }
        })) {
            _logger.LogInformation(
                "Iniciando creación de orden para cliente {CustomerId} con {LineCount} líneas",
                command.CustomerId, command.Lines.Count);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess) {
                _logger.LogWarning(
                    "Error al crear orden para cliente {CustomerId}: {Error}",
                    command.CustomerId, result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation(
                "Orden {OrderId} creada exitosamente para cliente {CustomerId}",
                result.Value, command.CustomerId);

            _metrics.OrderCreated(result.Value, command.CustomerId);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Value },
                new { orderId = result.Value });
        }
    }
}
