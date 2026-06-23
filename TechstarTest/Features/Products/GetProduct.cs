using MediatR;
using Microsoft.EntityFrameworkCore;
using TechstarTest.Infrastructure.Caching;
using TechstarTest.Infrastructure.Data;

namespace TechstarTest.Features.Products;

public record GetProductQuery(int Id) : IRequest<ProductDto?>;
public record ProductDto(int Id, string Name, decimal Price);

public class GetProductQueryHandler : IRequestHandler<GetProductQuery, ProductDto?>
{
    private readonly ApplicationDbContext _db;
    private readonly ICacheService _cache;

    public GetProductQueryHandler(ApplicationDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache;
    }
    public async Task<ProductDto?> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"Product:{request.Id}";

        return await _cache.GetOrSetAsync(
            key: cacheKey,
            factory: async (ct) =>
            {
                var product = await _db.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

                return product is null
                    ? null
                    : new ProductDto(product.Id, product.Name, product.Price);
            },
            expiry: TimeSpan.FromMinutes(5),
            ct: cancellationToken);
    }
}
