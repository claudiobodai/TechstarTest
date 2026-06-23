using MediatR;
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
