using PaymentApi.DTOs;
using PaymentApi.Models;
using PaymentApi.Repositories;
using PaymentApi.Configuration;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Diagnostics;

namespace PaymentApi.Services;

public interface IWebhookService
{
    Task<ApiResponse<WebhookDto>> CreateWebhookAsync(Guid merchantId, CreateWebhookRequest request);
    Task<ApiResponse<WebhookDto>> UpdateWebhookAsync(Guid webhookId, Guid merchantId, UpdateWebhookRequest request);
    Task<ApiResponse<bool>> DeleteWebhookAsync(Guid webhookId, Guid merchantId);
    Task<ApiResponse<IEnumerable<WebhookDto>>> GetMerchantWebhooksAsync(Guid merchantId);
    Task<ApiResponse<WebhookDto>> GetWebhookAsync(Guid webhookId, Guid merchantId);
    
    // Event publishing
    Task PublishWebhookEventAsync(string eventType, Guid paymentId, Guid merchantId, object eventData);
    Task ProcessWebhookEventAsync(WebhookEventDto webhookEvent);
}

public class WebhookService : IWebhookService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WebhookSettings _webhookSettings;
    private readonly RabbitMQSettings _rabbitMQSettings;
    private readonly ILogger<WebhookService> _logger;
    private readonly AutoMapper.IMapper _mapper;

    public WebhookService(
        IUnitOfWork unitOfWork,
        IRabbitMQService rabbitMQService,
        IHttpClientFactory httpClientFactory,
        WebhookSettings webhookSettings,
        RabbitMQSettings rabbitMQSettings,
        ILogger<WebhookService> logger,
        AutoMapper.IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _rabbitMQService = rabbitMQService;
        _httpClientFactory = httpClientFactory;
        _webhookSettings = webhookSettings;
        _rabbitMQSettings = rabbitMQSettings;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<ApiResponse<WebhookDto>> CreateWebhookAsync(Guid merchantId, CreateWebhookRequest request)
    {
        try
        {
            // Validate merchant exists
            if (!await _unitOfWork.Users.ExistsAsync(merchantId))
            {
                return ApiResponse<WebhookDto>.ErrorResponse("Merchant not found");
            }

            // Validate event types
            var invalidEventTypes = request.EventTypes.Except(WebhookEventTypes.AllEventTypes);
            if (invalidEventTypes.Any())
            {
                return ApiResponse<WebhookDto>.ErrorResponse($"Invalid event types: {string.Join(", ", invalidEventTypes)}");
            }

            var webhook = new Webhook
            {
                MerchantId = merchantId,
                WebhookUrl = request.WebhookUrl,
                IsActive = request.IsActive
            };
            
            webhook.SetEventTypes(request.EventTypes);

            await _unitOfWork.Webhooks.CreateAsync(webhook);
            await _unitOfWork.SaveChangesAsync();

            var webhookDto = _mapper.Map<WebhookDto>(webhook);
            
            _logger.LogInformation("Webhook {WebhookId} created for merchant {MerchantId}", webhook.WebhookId, merchantId);
            
            return ApiResponse<WebhookDto>.SuccessResponse(webhookDto, "Webhook created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating webhook for merchant {MerchantId}", merchantId);
            return ApiResponse<WebhookDto>.ErrorResponse("An error occurred while creating the webhook");
        }
    }

    public async Task<ApiResponse<WebhookDto>> UpdateWebhookAsync(Guid webhookId, Guid merchantId, UpdateWebhookRequest request)
    {
        try
        {
            var webhook = await _unitOfWork.Webhooks.GetWebhookByIdAndMerchantAsync(webhookId, merchantId);
            if (webhook == null)
            {
                return ApiResponse<WebhookDto>.ErrorResponse("Webhook not found");
            }

            if (!string.IsNullOrEmpty(request.WebhookUrl))
            {
                webhook.WebhookUrl = request.WebhookUrl;
            }

            if (request.EventTypes != null)
            {
                var invalidEventTypes = request.EventTypes.Except(WebhookEventTypes.AllEventTypes);
                if (invalidEventTypes.Any())
                {
                    return ApiResponse<WebhookDto>.ErrorResponse($"Invalid event types: {string.Join(", ", invalidEventTypes)}");
                }
                webhook.SetEventTypes(request.EventTypes);
            }

            if (request.IsActive.HasValue)
            {
                webhook.IsActive = request.IsActive.Value;
            }

            webhook.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Webhooks.UpdateAsync(webhook);
            await _unitOfWork.SaveChangesAsync();

            var webhookDto = _mapper.Map<WebhookDto>(webhook);
            
            _logger.LogInformation("Webhook {WebhookId} updated for merchant {MerchantId}", webhookId, merchantId);
            
            return ApiResponse<WebhookDto>.SuccessResponse(webhookDto, "Webhook updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating webhook {WebhookId} for merchant {MerchantId}", webhookId, merchantId);
            return ApiResponse<WebhookDto>.ErrorResponse("An error occurred while updating the webhook");
        }
    }

    public async Task<ApiResponse<bool>> DeleteWebhookAsync(Guid webhookId, Guid merchantId)
    {
        try
        {
            var webhook = await _unitOfWork.Webhooks.GetWebhookByIdAndMerchantAsync(webhookId, merchantId);
            if (webhook == null)
            {
                return ApiResponse<bool>.ErrorResponse("Webhook not found");
            }

            await _unitOfWork.Webhooks.DeleteAsync(webhook);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Webhook {WebhookId} deleted for merchant {MerchantId}", webhookId, merchantId);
            
            return ApiResponse<bool>.SuccessResponse(true, "Webhook deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting webhook {WebhookId} for merchant {MerchantId}", webhookId, merchantId);
            return ApiResponse<bool>.ErrorResponse("An error occurred while deleting the webhook");
        }
    }

    public async Task<ApiResponse<IEnumerable<WebhookDto>>> GetMerchantWebhooksAsync(Guid merchantId)
    {
        try
        {
            var webhooks = await _unitOfWork.Webhooks.GetWebhooksByMerchantAsync(merchantId);
            var webhookDtos = _mapper.Map<IEnumerable<WebhookDto>>(webhooks);
            
            return ApiResponse<IEnumerable<WebhookDto>>.SuccessResponse(webhookDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhooks for merchant {MerchantId}", merchantId);
            return ApiResponse<IEnumerable<WebhookDto>>.ErrorResponse("An error occurred while retrieving webhooks");
        }
    }

    public async Task<ApiResponse<WebhookDto>> GetWebhookAsync(Guid webhookId, Guid merchantId)
    {
        try
        {
            var webhook = await _unitOfWork.Webhooks.GetWebhookByIdAndMerchantAsync(webhookId, merchantId);
            if (webhook == null)
            {
                return ApiResponse<WebhookDto>.ErrorResponse("Webhook not found");
            }

            var webhookDto = _mapper.Map<WebhookDto>(webhook);
            return ApiResponse<WebhookDto>.SuccessResponse(webhookDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving webhook {WebhookId} for merchant {MerchantId}", webhookId, merchantId);
            return ApiResponse<WebhookDto>.ErrorResponse("An error occurred while retrieving the webhook");
        }
    }

    public async Task PublishWebhookEventAsync(string eventType, Guid paymentId, Guid merchantId, object eventData)
    {
        try
        {
            if (!_webhookSettings.EnableWebhooks)
            {
                _logger.LogDebug("Webhooks are disabled, skipping event publication");
                return;
            }

            // Create webhook event
            var webhookEvent = new WebhookEvent
            {
                PaymentId = paymentId,
                MerchantId = merchantId,
                EventType = eventType,
                EventData = JsonSerializer.Serialize(eventData),
                EventId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow
            };

            // Save to database
            await _unitOfWork.WebhookEvents.CreateAsync(webhookEvent);
            await _unitOfWork.SaveChangesAsync();

            // Create DTO for message queue
            var webhookEventDto = new WebhookEventDto
            {
                EventType = eventType,
                PaymentId = paymentId,
                MerchantId = merchantId,
                Data = eventData,
                Timestamp = webhookEvent.Timestamp,
                EventId = webhookEvent.EventId
            };

            // Publish to RabbitMQ
            await _rabbitMQService.PublishAsync(
                _rabbitMQSettings.PaymentWebhookExchange,
                _rabbitMQSettings.PaymentWebhookRoutingKey,
                webhookEventDto);

            _logger.LogInformation("Webhook event {EventType} published for payment {PaymentId}", eventType, paymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing webhook event {EventType} for payment {PaymentId}", eventType, paymentId);
        }
    }

    public async Task ProcessWebhookEventAsync(WebhookEventDto webhookEvent)
    {
        try
        {
            // Get merchant webhooks that should receive this event
            var webhooks = await _unitOfWork.Webhooks.GetActiveWebhooksForEventAsync(webhookEvent.MerchantId, webhookEvent.EventType);

            foreach (var webhook in webhooks)
            {
                await DeliverWebhookAsync(webhook, webhookEvent);
            }

            // Mark event as processed
            var storedEvent = await _unitOfWork.WebhookEvents.GetByEventIdAsync(webhookEvent.EventId);
            if (storedEvent != null)
            {
                storedEvent.IsProcessed = true;
                storedEvent.ProcessedAt = DateTime.UtcNow;
                await _unitOfWork.WebhookEvents.UpdateAsync(storedEvent);
                await _unitOfWork.SaveChangesAsync();
            }

            _logger.LogInformation("Webhook event {EventId} processed successfully", webhookEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event {EventId}", webhookEvent.EventId);
        }
    }

    private async Task DeliverWebhookAsync(Webhook webhook, WebhookEventDto webhookEvent)
    {
        var maxRetries = _webhookSettings.MaxRetryAttempts;
        var attempt = 1;
        var delay = _webhookSettings.InitialRetryDelaySeconds;

        while (attempt <= maxRetries)
        {
            try
            {
                var deliveryAttempt = new Models.WebhookDeliveryAttempt
                {
                    WebhookEventId = Guid.Parse(webhookEvent.EventId),
                    WebhookUrl = webhook.WebhookUrl,
                    AttemptNumber = attempt,
                    AttemptedAt = DateTime.UtcNow
                };

                var success = await SendWebhookRequestAsync(webhook.WebhookUrl, webhookEvent, deliveryAttempt);

                // Save delivery attempt
                await _unitOfWork.WebhookDeliveryAttempts.CreateAsync(deliveryAttempt);
                await _unitOfWork.SaveChangesAsync();

                if (success)
                {
                    _logger.LogInformation("Webhook delivered successfully to {WebhookUrl} for event {EventId} (attempt {Attempt})",
                        webhook.WebhookUrl, webhookEvent.EventId, attempt);
                    return;
                }

                if (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(delay));
                    delay = Math.Min(delay * 2, _webhookSettings.MaxRetryDelaySeconds); // Exponential backoff
                }

                attempt++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delivering webhook to {WebhookUrl} for event {EventId} (attempt {Attempt})",
                    webhook.WebhookUrl, webhookEvent.EventId, attempt);
                
                if (attempt >= maxRetries)
                {
                    break;
                }
                
                attempt++;
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }
        }

        _logger.LogWarning("Failed to deliver webhook to {WebhookUrl} for event {EventId} after {MaxRetries} attempts",
            webhook.WebhookUrl, webhookEvent.EventId, maxRetries);
    }

    private async Task<bool> SendWebhookRequestAsync(string webhookUrl, WebhookEventDto webhookEvent, Models.WebhookDeliveryAttempt deliveryAttempt)
    {
        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(_webhookSettings.TimeoutSeconds);

        var payload = new
        {
            eventType = webhookEvent.EventType,
            eventId = webhookEvent.EventId,
            timestamp = webhookEvent.Timestamp,
            data = webhookEvent.Data
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Add signature header for security (optional enhancement)
        // content.Headers.Add("X-Webhook-Signature", GenerateSignature(json));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await httpClient.PostAsync(webhookUrl, content);
            stopwatch.Stop();

            deliveryAttempt.StatusCode = (int)response.StatusCode;
            deliveryAttempt.ResponseBody = await response.Content.ReadAsStringAsync();
            deliveryAttempt.ResponseTime = stopwatch.Elapsed;
            deliveryAttempt.IsSuccessful = response.IsSuccessStatusCode;

            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            deliveryAttempt.StatusCode = 0;
            deliveryAttempt.ErrorMessage = ex.Message;
            deliveryAttempt.ResponseTime = stopwatch.Elapsed;
            deliveryAttempt.IsSuccessful = false;
            return false;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            stopwatch.Stop();
            deliveryAttempt.StatusCode = 408; // Request Timeout
            deliveryAttempt.ErrorMessage = "Request timeout";
            deliveryAttempt.ResponseTime = stopwatch.Elapsed;
            deliveryAttempt.IsSuccessful = false;
            return false;
        }
    }
}
