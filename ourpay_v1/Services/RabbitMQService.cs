using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using PaymentApi.Configuration;
using System.Text;
using System.Text.Json;

namespace PaymentApi.Services;

public interface IRabbitMQService
{
    Task PublishAsync<T>(string queueName, T message);
    Task PublishAsync<T>(string exchange, string routingKey, T message);
    void StartConsuming<T>(string queueName, Func<T, Task> messageHandler);
    void StopConsuming();
}

public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly IConnection? _connection;
    private readonly IModel? _channel;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQService> _logger;
    private readonly List<string> _consumerTags = new();

    public RabbitMQService(RabbitMQSettings settings, ILogger<RabbitMQService> logger)
    {
        _settings = settings;
        _logger = logger;

        try
        {
            var factory = new ConnectionFactory();
            
            if (!string.IsNullOrEmpty(_settings.ConnectionString))
            {
                // Use connection string if provided
                factory.Uri = new Uri(_settings.ConnectionString);
            }
            else
            {
                // Use individual properties
                factory.HostName = _settings.HostName;
                factory.Port = _settings.Port;
                factory.UserName = _settings.UserName;
                factory.Password = _settings.Password;
                factory.VirtualHost = _settings.VirtualHost;
                
                if (_settings.UseSSL)
                {
                    factory.Ssl = new SslOption
                    {
                        Enabled = true,
                        ServerName = _settings.HostName
                    };
                }
            }

            // Connection and channel setup
            factory.AutomaticRecoveryEnabled = true;
            factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange and queue for webhooks
            DeclareInfrastructure();

            _logger.LogInformation("RabbitMQ connection established successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to establish RabbitMQ connection");
            // Don't throw - allow the service to start without RabbitMQ
            // RabbitMQ features will be unavailable but the API will still function
        }
    }

    private void DeclareInfrastructure()
    {
        if (_channel == null) return;

        // Declare exchange
        _channel.ExchangeDeclare(
            exchange: _settings.PaymentWebhookExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        // Declare queue
        _channel.QueueDeclare(
            queue: _settings.PaymentWebhookQueue,
            durable: true,
            exclusive: false,
            autoDelete: false);

        // Bind queue to exchange
        _channel.QueueBind(
            queue: _settings.PaymentWebhookQueue,
            exchange: _settings.PaymentWebhookExchange,
            routingKey: _settings.PaymentWebhookRoutingKey);

        _logger.LogInformation("RabbitMQ infrastructure declared successfully");
    }

    public async Task PublishAsync<T>(string queueName, T message)
    {
        if (_channel == null)
        {
            _logger.LogWarning("RabbitMQ not available - message will not be published to queue {QueueName}", queueName);
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.MessageId = Guid.NewGuid().ToString();

            _channel.BasicPublish(
                exchange: "",
                routingKey: queueName,
                basicProperties: properties,
                body: body);

            _logger.LogDebug("Message published to queue {QueueName}: {MessageId}", queueName, properties.MessageId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to queue {QueueName}", queueName);
            throw;
        }
    }

    public async Task PublishAsync<T>(string exchange, string routingKey, T message)
    {
        if (_channel == null)
        {
            _logger.LogWarning("RabbitMQ not available - message will not be published to exchange {Exchange} with routing key {RoutingKey}", exchange, routingKey);
            return;
        }

        try
        {
            var json = JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.MessageId = Guid.NewGuid().ToString();

            _channel.BasicPublish(
                exchange: exchange,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogDebug("Message published to exchange {Exchange} with routing key {RoutingKey}: {MessageId}", 
                exchange, routingKey, properties.MessageId);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to exchange {Exchange}", exchange);
            throw;
        }
    }

    public void StartConsuming<T>(string queueName, Func<T, Task> messageHandler)
    {
        if (_channel == null)
        {
            _logger.LogWarning("RabbitMQ not available - cannot start consuming from queue {QueueName}", queueName);
            return;
        }

        try
        {
            var consumer = new EventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<T>(json);

                    if (message != null)
                    {
                        await messageHandler(message);
                        _channel.BasicAck(ea.DeliveryTag, false);
                        _logger.LogDebug("Message processed successfully from queue {QueueName}", queueName);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to deserialize message from queue {QueueName}", queueName);
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                    _channel.BasicNack(ea.DeliveryTag, false, true); // Requeue on error
                }
            };

            var consumerTag = _channel.BasicConsume(
                queue: queueName,
                autoAck: false,
                consumer: consumer);

            _consumerTags.Add(consumerTag);
            _logger.LogInformation("Started consuming messages from queue {QueueName}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start consuming from queue {QueueName}", queueName);
            throw;
        }
    }

    public void StopConsuming()
    {
        if (_channel == null)
        {
            _logger.LogWarning("RabbitMQ not available - cannot stop consuming");
            return;
        }

        try
        {
            foreach (var consumerTag in _consumerTags)
            {
                _channel.BasicCancel(consumerTag);
            }
            _consumerTags.Clear();
            _logger.LogInformation("Stopped consuming messages");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping message consumption");
        }
    }

    public void Dispose()
    {
        try
        {
            StopConsuming();
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
            _logger.LogInformation("RabbitMQ connection disposed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }
    }
}
