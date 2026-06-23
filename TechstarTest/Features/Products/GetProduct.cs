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
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);

    public GetProductQueryHandler(ApplicationDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }
    public async Task<ProductDto?> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"Product: {request.ProductId}";
        
        // cerca in cache
        var cached = await _cache.GetAsync<ProductDto>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        // se non trovato in cache, cerca nel database
        var product = await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null) return null;

        var dto = new ProductDto(product.Id, product.Name, product.Price);

        // scrivi in cache
        await _cache.SetAsync(cacheKey, dto, CacheExpiry, cancellationToken);

        return dto;
    }
}
