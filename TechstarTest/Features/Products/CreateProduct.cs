using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

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
   private readonly IMediator mediator;

    // Db context 

    public CreateProductCommandHandler(IMediator mediator)
    {
        this.mediator = mediator;
    }

    public async Task<int> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
       
        int newProductId = new Random().Next(1, 1000); 

        await mediator.Publish(new ProductCreatedEvent(newProductId, request.Name), cancellationToken);
       
        return newProductId;
    }
}

public class ProductCreatedEventHandler : INotificationHandler<ProductCreatedEvent>
{
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(ProductCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Evento di Dominio: Creato con successo il prodotto '{ProductName}' con ID {ProductId}",
            notification.Name, notification.ProductId);

        await Task.CompletedTask;
    }
}