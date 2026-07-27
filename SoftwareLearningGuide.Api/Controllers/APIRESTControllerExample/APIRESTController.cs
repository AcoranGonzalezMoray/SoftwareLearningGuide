using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace SoftwareLearningGuide.Api.Controllers.APIRESTControllerExample {
    [ApiController]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class APIRESTController : ControllerBase {
        // In-memory store for illustration purposes
        private static readonly ConcurrentDictionary<int, ItemDto> _store = new() {
            [1] = new ItemDto { Id = 1, Name = "Manzana", Description = "Fruta roja", CreatedAt = DateTime.UtcNow },
            [2] = new ItemDto { Id = 2, Name = "Pera", Description = "Fruta verde", CreatedAt = DateTime.UtcNow }
        };

        private static int _nextId = 3;

        // GET api/apirest
        // Ejemplo: filtros por query, paginado y orden
        [HttpGet]
        public ActionResult<IEnumerable<ItemDto>> GetAll([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();

            var items = _store.Values.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(q)) {
                items = items.Where(x => x.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || (x.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            items = items.OrderBy(x => x.Id)
                         .Skip((page - 1) * pageSize)
                         .Take(pageSize);

            return Ok(items);
        }

        // GET api/apirest/5
        [HttpGet("{id:int}")]
        public ActionResult<ItemDto> GetById([FromRoute] int id) {
            if (_store.TryGetValue(id, out var item))
                return Ok(item);

            return NotFound();
        }

        // GET api/apirest/headers -> lee un header personalizado
        [HttpGet("headers")]
        public ActionResult ReadHeader([FromHeader(Name = "X-Request-ID")] string? requestId) {
            if (string.IsNullOrEmpty(requestId))
                return BadRequest("Falta el header X-Request-ID");

            return Ok(new { RequestId = requestId });
        }

        // POST api/apirest
        [HttpPost]
        public ActionResult<ItemDto> Create([FromBody][Required] CreateItemDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var id = Interlocked.Increment(ref _nextId);
            var item = new ItemDto {
                Id = id,
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _store[item.Id] = item;

            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }

        // PUT api/apirest/5 -> reemplaza la entidad completa
        [HttpPut("{id:int}")]
        public ActionResult<ItemDto> Replace([FromRoute] int id, [FromBody][Required] CreateItemDto dto) {
            if (!_store.ContainsKey(id))
                return NotFound();

            var item = new ItemDto {
                Id = id,
                Name = dto.Name,
                Description = dto.Description,
                CreatedAt = DateTime.UtcNow
            };

            _store[id] = item;

            return Ok(item);
        }

        // PATCH api/apirest/5 -> actualización parcial simple (ilustrativa)
        [HttpPatch("{id:int}")]
        public ActionResult<ItemDto> Patch([FromRoute] int id, [FromBody] JsonElement patchBody) {
            if (!_store.TryGetValue(id, out var item))
                return NotFound();

            // Soporta propiedades: name, description
            if (patchBody.ValueKind != JsonValueKind.Object)
                return BadRequest("Body debe ser un objeto JSON con propiedades a actualizar.");

            if (patchBody.TryGetProperty("name", out var nameProp) && nameProp.ValueKind == JsonValueKind.String) {
                item.Name = nameProp.GetString()!;
            }

            if (patchBody.TryGetProperty("description", out var descProp) && descProp.ValueKind == JsonValueKind.String) {
                item.Description = descProp.GetString();
            }

            _store[item.Id] = item;

            return Ok(item);
        }

        // DELETE api/apirest/5
        [HttpDelete("{id:int}")]
        public ActionResult Delete([FromRoute] int id) {
            if (!_store.TryRemove(id, out _))
                return NotFound();

            return NoContent();
        }

        // POST api/apirest/upload -> subir un fichero por form-data
        [HttpPost("upload")]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB ilustrativo
        public async Task<ActionResult> UploadFile([FromForm] IFormFile file, CancellationToken cancellationToken) {
            if (file == null || file.Length == 0)
                return BadRequest("Fichero vacío");

            // No guardamos en disco en este ejemplo, solo leemos algunos bytes
            using var stream = file.OpenReadStream();
            var buffer = new byte[Math.Min((int)file.Length, 1024)];
            await stream.ReadExactlyAsync(buffer, cancellationToken);

            return Ok(new { file.FileName, file.Length, sample = Convert.ToBase64String(buffer) });
        }

        // Ejemplos de respuestas de error/estado
        [HttpGet("forbidden")]
        public ActionResult ForbiddenExample() => Forbid();

        [HttpGet("unauthorized")]
        public ActionResult UnauthorizedExample() => Unauthorized();

        [HttpGet("problem")]
        public ActionResult ProblemExample() => Problem(detail: "Ejemplo de ProblemDetails", statusCode: StatusCodes.Status500InternalServerError);

        // Ejemplo de streaming simple (IAsyncEnumerable)
        [HttpGet("stream")]
        public async IAsyncEnumerable<ItemDto> StreamAll([FromQuery] int count = 10) {
            for (var i = 0; i < count; i++) {
                await Task.Delay(100);
                yield return new ItemDto { Id = 1000 + i, Name = $"Item_{i}", Description = "Generado en stream", CreatedAt = DateTime.UtcNow };
            }
        }
    }

    // DTOs y modelos usados en el controlador (solo ilustración)
    public record ItemDto {
        public int Id { get; init; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; init; }
    }

    public record CreateItemDto {
        [Required]
        public string Name { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}
