using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using TechstarTest.Domain.Entities;
using TechstarTest.Features.Products.Notifications;
using TechstarTest.Infrastructure.Data;
using TechstarTest.Infrastructure.Notifications;

namespace TechstarTest.Features.Products;

public record CreateProductCommand(string Name, decimal Price) : IRequest<int>;
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Il Nome è obbligatorio.")
            .MaximumLength(100);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Il prezzo deve essere maggiore di zero.");
    }
}

public record ProductCreatedEvent(int ProductId, string Name) : INotification;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, int>
{
    private readonly IMediator _mediator;
    private readonly ApplicationDbContext _db;
    private readonly IProductNotificationService _notificationService;

    public CreateProductCommandHandler(IMediator mediator, ApplicationDbContext db, IProductNotificationService notificationService)
    {
        _mediator = mediator;
        _db = db;
        _notificationService = notificationService;

    }

    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product(request.Name, request.Price);

        _db.Products.Add(product);

        await _db.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyProductCreatedAsync(product.Id, product.Name);

        await _mediator.Publish(new ProductCreatedEvent(product.Id, product.Name), cancellationToken);
        
        return product.Id;
    }
}

public class ProductCreatedEventHandler : INotificationHandler<ProductCreatedEvent>
{
    private readonly ILogger<ProductCreatedEventHandler> _logger;
    private readonly INotificationFactory _notificationFactory;


    public ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger, INotificationFactory notificationFactory)
    {
        _logger = logger;
        _notificationFactory = notificationFactory;
    } 

    public async Task Handle(ProductCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Evento di Dominio: Creato con successo il prodotto '{ProductName}' con ID {ProductId}",
            notification.Name, notification.ProductId);

        var sender = _notificationFactory.Create("email");
        await sender.Send($"Nuovo prodotto disponibile: {notification.Name}");

        await Task.CompletedTask;
    }
}