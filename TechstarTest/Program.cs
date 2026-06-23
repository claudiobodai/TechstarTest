using FluentValidation;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TechstarTest.Behaviors;
using TechstarTest.Features.Products.Notifications;
using TechstarTest.Infrastructure.Caching;
using TechstarTest.Infrastructure.Data;
using TechstarTest.Infrastructure.Exceptions;
using TechstarTest.Infrastructure.Notifications;
using Microsoft.AspNetCore.Mvc.NewtonsoftJson;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
    .AddNewtonsoftJson();

builder.Services.AddOpenApi();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

builder.Services.AddScoped<ProductNotificationService>();
builder.Services.AddScoped<IProductNotificationService>(sp =>
    new LoggingProductNotificationDecorator(
        sp.GetRequiredService<ProductNotificationService>(),   
        sp.GetRequiredService<ILogger<LoggingProductNotificationDecorator>>()
    ));

builder.Services.AddScoped<EmailSender>();
builder.Services.AddScoped<SmsSender>();
builder.Services.AddScoped<PushSender>();

builder.Services.AddScoped<INotificationFactory>(sp =>
{
    var registry = new Dictionary<string, Func<INotificationSender>>
    {
        ["email"] = () => sp.GetRequiredService<EmailSender>(),
        ["sms"] = () => sp.GetRequiredService<SmsSender>(),
        ["push"] = () => sp.GetRequiredService<PushSender>(),  
    };

    return new NotificationFactory(registry);
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
