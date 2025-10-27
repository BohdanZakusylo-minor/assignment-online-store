using Microsoft.EntityFrameworkCore;
using OrderManagerService.Models;
using OrderManagerService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add EventService
builder.Services.AddSingleton<EventService>();

var app = builder.Build();

// Start listening to validation events
var eventService = app.Services.GetRequiredService<EventService>();
var dbContext = app.Services.CreateScope().ServiceProvider.GetRequiredService<OrderDbContext>();

eventService.StartListeningOrderValidated(async (validatedEvent) =>
{
    try
    {
        var order = await dbContext.Orders.FindAsync(validatedEvent.OrderId);
        if (order != null)
        {
            if (!validatedEvent.IsValid)
            {
                // Delete invalid order
                dbContext.Orders.Remove(order);
                await dbContext.SaveChangesAsync();
                app.Logger.LogInformation($"Order {validatedEvent.OrderId} rejected due to invalid products");
            }
            else
            {
                app.Logger.LogInformation($"Order {validatedEvent.OrderId} validated successfully");
                
                // Wait 5 seconds, then auto-ship the order
                await Task.Delay(5000);
                
                // Set shipment date to current time
                order.ShipmentDate = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                
                app.Logger.LogInformation($"Order {validatedEvent.OrderId} shipped automatically");
            }
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error handling order validated event");
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
