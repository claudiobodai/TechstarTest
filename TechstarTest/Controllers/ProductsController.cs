using MediatR;
using Microsoft.AspNetCore.Mvc;
using TechstarTest.Features.Products;

namespace TechstarTest.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command)
    {
        var productId = await _mediator.Send(command);
        return Created($"/api/products/{productId}", new { Id = productId });
    }
}
