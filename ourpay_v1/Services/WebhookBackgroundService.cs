using PaymentApi.DTOs;
using PaymentApi.Services;
using PaymentApi.Configuration;
using PaymentApi.Repositories;

namespace PaymentApi.Services;

public class WebhookBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly RabbitMQSettings _rabbitMQSettings;
    private readonly ILogger<WebhookBackgroundService> _logger;

    public WebhookBackgroundService(
        IServiceProvider serviceProvider,
        IRabbitMQService rabbitMQService,
        RabbitMQSettings rabbitMQSettings,
        ILogger<WebhookBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _rabbitMQService = rabbitMQService;
        _rabbitMQSettings = rabbitMQSettings;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Webhook Background Service started");

        // Start consuming webhook events from RabbitMQ
        _rabbitMQService.StartConsuming<WebhookEventDto>(
            _rabbitMQSettings.PaymentWebhookQueue,
            ProcessWebhookEventAsync);

        // Keep the service running
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            // Optionally process any unprocessed events from database
            await ProcessUnprocessedEventsAsync();
        }

        _logger.LogInformation("Webhook Background Service stopped");
    }

    private async Task ProcessWebhookEventAsync(WebhookEventDto webhookEvent)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();

            await webhookService.ProcessWebhookEventAsync(webhookEvent);

            _logger.LogDebug("Processed webhook event {EventId} for payment {PaymentId}", 
                webhookEvent.EventId, webhookEvent.PaymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event {EventId}", webhookEvent.EventId);
        }
    }

    private async Task ProcessUnprocessedEventsAsync()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var webhookService = scope.ServiceProvider.GetRequiredService<IWebhookService>();

            var unprocessedEvents = await unitOfWork.WebhookEvents.GetUnprocessedEventsAsync();

            foreach (var webhookEvent in unprocessedEvents)
            {
                try
                {
                    var webhookEventDto = new WebhookEventDto
                    {
                        EventType = webhookEvent.EventType,
                        PaymentId = webhookEvent.PaymentId,
                        MerchantId = webhookEvent.MerchantId,
                        Data = System.Text.Json.JsonSerializer.Deserialize<object>(webhookEvent.EventData) ?? new object(),
                        Timestamp = webhookEvent.Timestamp,
                        EventId = webhookEvent.EventId
                    };

                    await webhookService.ProcessWebhookEventAsync(webhookEventDto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing unprocessed webhook event {EventId}", webhookEvent.EventId);
                }
            }

            if (unprocessedEvents.Any())
            {
                _logger.LogInformation("Processed {Count} unprocessed webhook events", unprocessedEvents.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing unprocessed webhook events");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stopping Webhook Background Service");
        _rabbitMQService.StopConsuming();
        await base.StopAsync(stoppingToken);
    }
}
