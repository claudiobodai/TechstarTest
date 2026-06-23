using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using TechstarTest.Infrastructure.Caching;
using TechstarTest.Infrastructure.Data;

namespace TechstarTest.Features.Products;

public record UpdateProductCommand(int Id, string Name, decimal Price) : IRequest<bool>;

public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Il Nome è obbligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Il prezzo deve essere maggiore di zero.");
    }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, bool>
{
    private readonly ApplicationDbContext _db;

    public UpdateProductCommandHandler(ApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FindAsync([request.Id], cancellationToken);
        if (product is null) return false;

        product.Name = request.Name;
        product.Price = request.Price;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}


public record PatchProductCommand(int Id, JsonPatchDocument<PatchProductDto> PatchDocument) : IRequest<bool>;

public class PatchProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class PatchProductCommandHandler : IRequestHandler<PatchProductCommand, bool>
{
    private readonly ApplicationDbContext _db;
    private readonly ICacheService _cache;

    public PatchProductCommandHandler(ApplicationDbContext db, ICacheService cache)
    {
        _db = db;
        _cache = cache; 
    }
    public async Task<bool> Handle(PatchProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FindAsync([request.Id], cancellationToken);
        if (product is null) return false;

        var dto = new PatchProductDto
        {
            Name = product.Name,
            Price = product.Price
        };

        request.PatchDocument.ApplyTo(dto);

        if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0)
            return false;

        product.Name = dto.Name;
        product.Price = dto.Price;

        await _db.SaveChangesAsync(cancellationToken);

        await _cache.RemoveAsync($"Product:{request.Id}", cancellationToken);
        return true;
    }
}