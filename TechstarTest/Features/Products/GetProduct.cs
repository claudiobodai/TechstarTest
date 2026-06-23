using MediatR;
using Microsoft.EntityFrameworkCore;
using TechstarTest.Infrastructure.Caching;
using TechstarTest.Infrastructure.Data;

namespace TechstarTest.Features.Products;

public record GetProductQuery(int ProductId) : IRequest<ProductDto?>;
public record ProductDto(int Id, string Name, decimal Price);

public class GetProductQueryHandler : IRequestHandler<GetProductQuery, ProductDto?>
{
    private readonly ApplicationDbContext _db;
    private readonly ICacheService _cache;
    private readonly ILogger<GetProductQueryHandler> _logger;
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);

    public GetProductQueryHandler(ApplicationDbContext db, ICacheService cache, ILogger<GetProductQueryHandler> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }
    public async Task<ProductDto?> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"Product: {request.ProductId}";
        
        var cached = await _cache.GetAsync<ProductDto>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogInformation("Product {ProductId} found in cache.", request.ProductId);
            return cached;
        }

        _logger.LogInformation("Product {ProductId} not found in cache. Querying database.", request.ProductId);
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null) return null;

        var dto = new ProductDto(product.Id, product.Name, product.Price);

        await _cache.SetAsync(cacheKey, dto, CacheExpiry, cancellationToken);
        _logger.LogInformation("Product {ProductId} cached for {CacheExpiry} minutes.", request.ProductId, CacheExpiry.TotalMinutes);

        return dto;
    }
}
