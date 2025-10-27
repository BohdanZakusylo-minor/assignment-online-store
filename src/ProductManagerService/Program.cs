using Microsoft.EntityFrameworkCore;
using ProductManagerService.Models;
using ProductManagerService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add ImageService for blob storage
builder.Services.AddScoped<ImageService>();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<EventService>>();

EventService? eventServiceInstance = null;

async Task HandleOrderCreated(OrderCreatedEvent orderEvent)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    
    var invalidProducts = new List<int>();
    
    foreach (var productId in orderEvent.ProductsId)
    {
        var productExists = await dbContext.Products.AnyAsync(p => p.ProductId == productId);
        if (!productExists)
        {
            invalidProducts.Add(productId);
        }
    }
    
    var validatedEvent = new OrderValidatedEvent
    {
        OrderId = orderEvent.OrderId,
        IsValid = invalidProducts.Count == 0,
        InvalidProducts = invalidProducts
    };
    
    eventServiceInstance?.PublishOrderValidated(validatedEvent);
    
    app.Logger.LogInformation($"Validated Order {orderEvent.OrderId}: Valid={validatedEvent.IsValid}, InvalidProducts={string.Join(",", invalidProducts)}");
}

eventServiceInstance = new EventService(logger, HandleOrderCreated);

eventServiceInstance.StartListeningOrderCreated();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
