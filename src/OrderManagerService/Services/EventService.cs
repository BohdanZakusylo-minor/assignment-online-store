using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using OrderManagerService.Models;

namespace OrderManagerService.Services;

public class EventService : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<EventService> _logger;

    public EventService(ILogger<EventService> logger)
    {
        _logger = logger;
        
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

    public void PublishOrderCreated(OrderCreatedEvent orderEvent)
    {
        try
        {
            var message = JsonSerializer.Serialize(orderEvent);
            var body = Encoding.UTF8.GetBytes(message);

            _channel.BasicPublish(
                exchange: "",
                routingKey: "order.created",
                basicProperties: null,
                body: body);
            
            _logger.LogInformation($"Published OrderCreated event for Order {orderEvent.OrderId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing order created event");
        }
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

    public void StartListeningOrderValidated(Action<OrderValidatedEvent> handler)
    {
        var consumer = new RabbitMQ.Client.Events.EventingBasicConsumer(_channel);
        
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            try
            {
                var validatedEvent = JsonSerializer.Deserialize<OrderValidatedEvent>(message);
                if (validatedEvent != null)
                {
                    handler(validatedEvent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order validated event");
            }
        };
        
        _channel.BasicConsume(queue: "order.validated", autoAck: true, consumer: consumer);
        _logger.LogInformation("Started listening to order.validated queue");
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}

