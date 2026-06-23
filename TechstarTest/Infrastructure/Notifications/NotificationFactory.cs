namespace TechstarTest.Infrastructure.Notifications;

public interface INotificationSender
{
    Task Send(string message);
}

public class EmailSender : INotificationSender
{
    private readonly ILogger<EmailSender> _logger;
    public EmailSender(ILogger<EmailSender> logger) => _logger = logger;

    public Task Send(string message)
    {
        _logger.LogInformation("[EmailSender] Invio email: {Message}", message);
        return Task.CompletedTask;
    }
}

public class SmsSender : INotificationSender
{
    private readonly ILogger<SmsSender> _logger;
    public SmsSender(ILogger<SmsSender> logger) => _logger = logger;

    public Task Send(string message)
    {
        _logger.LogInformation("[SmsSender] Invio SMS: {Message}", message);
        return Task.CompletedTask;
    }
}

public class PushSender : INotificationSender
{
    private readonly ILogger<PushSender> _logger;
    public PushSender(ILogger<PushSender> logger) => _logger = logger;

    public Task Send(string message)
    {
        _logger.LogInformation("[PushSender] Invio push notification: {Message}", message);
        return Task.CompletedTask;
    }
}

public interface INotificationFactory
{
    INotificationSender Create(string channel);
}

public class NotificationFactory : INotificationFactory
{
    private readonly IReadOnlyDictionary<string, Func<INotificationSender>> _registry;

    public NotificationFactory(IReadOnlyDictionary<string, Func<INotificationSender>> registry)
    {
        _registry = registry;
    }

    public INotificationSender Create(string channel)
    {
        if (!_registry.TryGetValue(channel, out var factory))
            throw new NotSupportedException(
                $"Canale di notifica '{channel}' non supportato. " +
                $"Canali disponibili: {string.Join(", ", _registry.Keys)}");

        return factory();
    }
}