namespace TechstarTest.Features.Products.Notifications;

public interface IProductNotificationService
{
    Task NotifyProductCreatedAsync(int productId, string name, CancellationToken ct = default);
}
public class ProductNotificationService : IProductNotificationService
{
    public Task NotifyProductCreatedAsync(int productId, string name, CancellationToken ct = default)
    {
        // Simulazione di invio di una notifica (ad esempio, invio di un'email o pubblicazione su un bus di messaggi)
        Console.WriteLine($"Product created: Id={productId}, Name={name}");
        return Task.CompletedTask;
    }
}

public sealed class LoggingProductNotificationDecorator : IProductNotificationService
{
    private readonly IProductNotificationService _inner;
    private readonly ILogger<LoggingProductNotificationDecorator> _logger;


    public LoggingProductNotificationDecorator(IProductNotificationService inner, ILogger<LoggingProductNotificationDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task NotifyProductCreatedAsync(int productId, string name, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Decorator] NotifyProductCreatedAsync START | ProductId={ProductId} Name={Name}",
            productId, name);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            await _inner.NotifyProductCreatedAsync(productId, name, ct);
            sw.Stop();

            _logger.LogInformation(
                "[Decorator] NotifyProductCreatedAsync OK | ProductId={ProductId} [{Elapsed}ms]",
                productId, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[Decorator] NotifyProductCreatedAsync FAILED | ProductId={ProductId} [{Elapsed}ms]",
                productId, sw.ElapsedMilliseconds);
            throw; 
        }
    }
}
