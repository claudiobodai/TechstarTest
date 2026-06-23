using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TechstarTest.Behaviors;
using TechstarTest.Features.Products.Notifications;
using TechstarTest.Infrastructure.Data;
using TechstarTest.Infrastructure.Exceptions;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddScoped<ProductNotificationService>();
builder.Services.AddScoped<IProductNotificationService>(sp =>
    new LoggingProductNotificationDecorator(
        sp.GetRequiredService<ProductNotificationService>(),   
        sp.GetRequiredService<ILogger<LoggingProductNotificationDecorator>>()
    ));

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
