using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using SoftwareLearningGuide.Api.Extensions;
using SoftwareLearningGuide.Api.FeatureToggles;
using SoftwareLearningGuide.Application.Command.CreateCustomer;
using SoftwareLearningGuide.Application.Query.GetAllCustomer;
using SoftwareLearningGuide.Application.Query.GetCustomer;

namespace SoftwareLearningGuide.Api.Controllers.CustomerControllerExample;

/// <summary>
/// Controller de Customers que demuestra CQRS con MediatR.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = CognitoPolicies.RequireNormalRole)]
[FeatureGate(FeatureToggleNames.FT_ENABLE_CUSTOMER_CONTROLLER)]
[Route("api/v{version:apiVersion}/[controller]")]
public class CustomerController : ControllerBase {
    private readonly IMediator _mediator;
    private readonly ILogger<CustomerController> _logger;

    public CustomerController(IMediator mediator, ILogger<CustomerController> logger) {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// GET api/v1/customer
    /// Obtiene todos los clientes.
    /// </summary>
    [HttpGet]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_CUSTOMER_LIST)]
    [ProducesResponseType(typeof(GetAllCustomersQueryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", HttpContext.TraceIdentifier }
        })) {
            _logger.LogInformation("Iniciando obtencion de todos los clientes");

            var query = new GetAllCustomersQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess) {
                _logger.LogWarning("Error al obtener clientes: {Error}", result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation("Se obtuvieron {Count} clientes", result.Value!.TotalCount);
            return Ok(result.Value);
        }
    }

    /// <summary>
    /// GET api/v1/customer/{id}
    /// Obtiene un cliente por su ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_CUSTOMER_RETRIEVAL)]
    [ProducesResponseType(typeof(GetCustomerQueryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", HttpContext.TraceIdentifier },
            { "CustomerId", id }
        })) {
            _logger.LogInformation("Iniciando obtencion de cliente {CustomerId}", id);

            var query = new GetCustomerQuery(id);
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess) {
                _logger.LogWarning("Cliente {CustomerId} no encontrado: {Error}", id, result.Error);
                return NotFound(new { error = result.Error });
            }

            _logger.LogInformation("Cliente {CustomerId} obtenido exitosamente", id);
            return Ok(result.Value);
        }
    }

    /// <summary>
    /// POST api/v1/customer
    /// Crea un nuevo cliente.
    /// </summary>
    [HttpPost]
    [FeatureGate(FeatureToggleNames.FT_ENABLE_CUSTOMER_CREATION)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command, CancellationToken cancellationToken) {
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            { "CorrelationId", HttpContext.TraceIdentifier },
            { "FirstName", command.FirstName },
            { "LastName", command.LastName },
            { "Email", command.Email }
        })) {
            _logger.LogInformation(
                "Iniciando creacion de cliente {FirstName} {LastName}",
                command.FirstName, command.LastName);

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.IsSuccess) {
                _logger.LogWarning(
                    "Error al crear cliente {FirstName} {LastName}: {Error}",
                    command.FirstName, command.LastName, result.Error);
                return BadRequest(new { error = result.Error });
            }

            _logger.LogInformation(
                "Cliente {CustomerId} creado exitosamente",
                result.Value);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Value },
                new { customerId = result.Value });
        }
    }
}
