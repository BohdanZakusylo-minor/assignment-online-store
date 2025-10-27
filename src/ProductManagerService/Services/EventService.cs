using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using ProductManagerService.Models;

namespace ProductManagerService.Services;

public class EventService : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<EventService> _logger;
    private readonly Func<OrderCreatedEvent, Task> _onOrderCreated;

    public EventService(ILogger<EventService> logger, Func<OrderCreatedEvent, Task> onOrderCreated)
    {
        _logger = logger;
        _onOrderCreated = onOrderCreated;
        
        var factory = new ConnectionFactory 
        { 
            HostName = "rabbitmq",
            UserName = "rabbituser",
            Password = "rabbitpass",
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };
        
        // Retry connection with exponential backoff
        _connection = RetryConnection(factory);
        _channel = _connection.CreateModel();
        
        // Declare queues
        _channel.QueueDeclare(queue: "order.created", durable: true, exclusive: false, autoDelete: false);
        _channel.QueueDeclare(queue: "order.validated", durable: true, exclusive: false, autoDelete: false);
        
        _logger.LogInformation("EventService connected to RabbitMQ");
    }

    private IConnection RetryConnection(ConnectionFactory factory)
    {
        const int maxRetries = 10;
        const int delayMs = 2000;

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                _logger.LogInformation($"Attempting to connect to RabbitMQ (attempt {i + 1}/{maxRetries})...");
                return factory.CreateConnection();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to connect to RabbitMQ: {ex.Message}. Retrying in {delayMs}ms...");
                Thread.Sleep(delayMs);
            }
        }

        throw new Exception("Failed to connect to RabbitMQ after maximum retries");
    }

    public void StartListeningOrderCreated()
    {
        var consumer = new RabbitMQ.Client.Events.EventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            try
            {
                var orderEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message);
                if (orderEvent != null)
                {
                    await _onOrderCreated(orderEvent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order created event");
            }
        };
        
        _channel.BasicConsume(queue: "order.created", autoAck: true, consumer: consumer);
        _logger.LogInformation("Started listening to order.created queue");
    }

    public void PublishOrderValidated(OrderValidatedEvent validatedEvent)
    {
        try
        {
            var message = JsonSerializer.Serialize(validatedEvent);
            var body = Encoding.UTF8.GetBytes(message);

            _channel.BasicPublish(
                exchange: "",
                routingKey: "order.validated",
                basicProperties: null,
                body: body);
            
            _logger.LogInformation($"Published OrderValidated event for Order {validatedEvent.OrderId}: {validatedEvent.IsValid}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing order validated event");
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}

