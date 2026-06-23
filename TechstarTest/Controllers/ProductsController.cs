using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using TechstarTest.Features.Products;
using TechstarTest.Infrastructure.Caching;

namespace TechstarTest.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICacheService _cache;

    public ProductsController(IMediator mediator , ICacheService cache)
    {
        _mediator = mediator;
        _cache = cache;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command)
    {
        var productId = await _mediator.Send(command);
        return Created($"/api/products/{productId}", new { Id = productId });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _mediator.Send(new GetProductQuery(id));
        if (product is null) return NotFound();
        return Ok(product);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id)
            return BadRequest("L'id nell'URL non corrisponde all'id nel body.");

        var updated = await _mediator.Send(command);
        if (!updated) return NotFound();

        return NoContent(); 
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> PatchProduct(
       int id,
       [FromBody] JsonPatchDocument<PatchProductDto> patchDocument)
    {
        if (patchDocument is null)
            return BadRequest("Il documento patch non può essere null.");

        var updated = await _mediator.Send(new PatchProductCommand(id, patchDocument));
        if (!updated) return NotFound();

        return NoContent();
    }


    [HttpGet("cache/metrics")]
    public IActionResult GetCacheMetricsEndpoint()
    {
        var metrics = _cache.GetCacheMetrics(); 
        return Ok(new
        {
            hits = metrics.Hits,
            misses = metrics.Misses,
            hitRatio = metrics.HitRatio.ToString("P1")
        });
    }
}
